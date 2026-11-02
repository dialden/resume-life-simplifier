using System.Text.Json;

namespace DialDen.RLS.Pipelines;

public sealed class CloudRunClient(ProcessRunner processRunner, GoogleCloudConfig config)
{
    public async Task<string> DeployUi(string name, string image, string serviceAccount,
        string apiName, string apiUrl, string artifactsDirectory)
    {
        var environmentFile = Path.Combine(artifactsDirectory, "ui-environment.json");
        await File.WriteAllTextAsync(environmentFile,
            JsonSerializer.Serialize(new Dictionary<string, string> { ["API_BASE_URL"] = apiUrl }));
        await processRunner.Run("gcloud",
            ["run", "deploy", name, $"--project={config.ProjectId}", $"--region={config.Region}",
                $"--image={image}", $"--service-account={serviceAccount}",
                "--execution-environment=gen2", "--port=80", "--cpu=1", "--memory=512Mi",
                "--min-instances=0", "--max-instances=1", "--concurrency=80",
                "--ingress=all", "--allow-unauthenticated", $"--env-vars-file={environmentFile}",
                "--clear-secrets", "--clear-cloudsql-instances",
                "--startup-probe=httpGet.path=/,httpGet.port=80,periodSeconds=5,timeoutSeconds=3,failureThreshold=24",
                "--quiet"]);
        await processRunner.Run("gcloud",
            ["run", "services", "update-traffic", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--to-latest", "--quiet"]);
        var description = await processRunner.Run("gcloud",
            ["run", "services", "describe", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--format=json", "--quiet"], captureOutput: true);
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "ui.json"), description);
        using var service = JsonDocument.Parse(description);
        var url = service.RootElement.GetProperty("status").GetProperty("url").GetString();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"Cloud Run returned no valid HTTPS URL for UI '{name}'.");
        }
        await processRunner.Run("gcloud",
            ["run", "services", "update", apiName, $"--project={config.ProjectId}",
                $"--region={config.Region}", $"--update-env-vars=RLS_API_Cors__AllowedOrigins__0={url}", "--quiet"]);
        await processRunner.Run("gcloud",
            ["run", "services", "update-traffic", apiName, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--to-latest", "--quiet"]);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        using var settingsResponse = await client.GetAsync(new Uri(address, "/appsettings.json"));
        settingsResponse.EnsureSuccessStatusCode();
        using var settings = JsonDocument.Parse(await settingsResponse.Content.ReadAsStringAsync());
        if (settings.RootElement.GetProperty("ApiBaseUrl").GetString() != apiUrl)
        {
            throw new InvalidOperationException("The deployed UI does not reference the expected API URL.");
        }
        using var request = new HttpRequestMessage(HttpMethod.Options, apiUrl + "/postings");
        request.Headers.Add("Origin", url);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode ||
            !response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins) ||
            origins.Single() != url)
        {
            throw new InvalidOperationException($"API CORS verification failed for UI origin '{url}'.");
        }
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "ui-url.txt"), url + "\n");
        PipelineLog.Write($"UI deployed with API configuration and exact-origin CORS verified: {url}");
        return url;
    }

    public async Task<string> DeployApi(string name, string image, string connectionName,
        string serviceAccount, string connectionSecret, string artifactsDirectory)
    {
        var environmentFile = Path.Combine(artifactsDirectory, "api-environment.json");
        await File.WriteAllTextAsync(environmentFile, JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["RLS_API_URLS"] = "http://+:8080",
            ["RLS_API_Messaging__JobSitePublicationQueue"] = "rls-job-site-publication",
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true"
        }));
        await processRunner.Run("gcloud",
            ["run", "deploy", name, $"--project={config.ProjectId}", $"--region={config.Region}",
                $"--image={image}", $"--service-account={serviceAccount}",
                "--execution-environment=gen2", "--port=8080", "--cpu=1", "--memory=512Mi",
                "--min-instances=0", "--max-instances=1", "--concurrency=10", "--timeout=60",
                "--ingress=all", "--allow-unauthenticated",
                $"--set-cloudsql-instances={connectionName}", $"--env-vars-file={environmentFile}",
                $"--set-secrets=RLS_API_ConnectionStrings__RLS={connectionSecret}",
                "--startup-probe=httpGet.path=/health,httpGet.port=8080,periodSeconds=5,timeoutSeconds=3,failureThreshold=24",
                "--quiet"]);
        await processRunner.Run("gcloud",
            ["run", "services", "update-traffic", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--to-latest", "--quiet"]);
        var description = await processRunner.Run("gcloud",
            ["run", "services", "describe", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--format=json", "--quiet"], captureOutput: true);
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "api.json"), description);
        using var service = JsonDocument.Parse(description);
        var url = service.RootElement.GetProperty("status").GetProperty("url").GetString();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"Cloud Run returned no valid HTTPS URL for API '{name}'.");
        }
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        using var response = await client.GetAsync(new Uri(address, "/health"));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode || body.Trim() != "Healthy")
        {
            throw new InvalidOperationException(
                $"API health check failed at '{address}': HTTP {(int)response.StatusCode}, {body}.");
        }
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "api-url.txt"), url + "\n");
        PipelineLog.Write($"API deployed and public health check passed: {url}");
        return url;
    }

    public async Task RequireWorkerStopped(string name)
    {
        using var pools = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["beta", "run", "worker-pools", "list", $"--project={config.ProjectId}",
                $"--region={config.Region}", "--format=json", "--quiet"], captureOutput: true));
        if (pools.RootElement.EnumerateArray().Any(pool =>
            pool.GetProperty("metadata").GetProperty("name").GetString() == name &&
            (!pool.GetProperty("metadata").GetProperty("annotations")
                .TryGetProperty("run.googleapis.com/manualInstanceCount", out var count) || count.GetString() != "0")))
        {
            throw new InvalidOperationException(
                $"Worker pool '{name}' is not scaled to zero. Release stopped before migrations. " +
                "Scale it to zero and verify its instances have stopped before starting a maintenance-window release.");
        }
    }

    public async Task<string> DeployWorker(string name, string image, string connectionName,
        string serviceAccount, JobillicoConfig jobSite, string connectionSecret,
        string usernameSecret, string passwordSecret, string artifactsDirectory)
    {
        jobSite.Validate();
        var environment = new Dictionary<string, string>
        {
            ["RLS_WORKER_JobSites__Type"] = "real",
            ["RLS_WORKER_Jobillico__Endpoint"] = jobSite.Endpoint,
            ["RLS_WORKER_Jobillico__RecruiterId"] = jobSite.RecruiterId,
            ["RLS_WORKER_Jobillico__LocationId"] = jobSite.LocationId,
            ["RLS_WORKER_Jobillico__UseProduction"] = jobSite.UseProduction.ToString(),
            ["RLS_WORKER_Messaging__JobSitePublicationQueue"] = "rls-job-site-publication"
        };
        var definition = new
        {
            apiVersion = "run.googleapis.com/v1",
            kind = "WorkerPool",
            metadata = new
            {
                name,
                @namespace = config.ProjectNumber,
                annotations = new Dictionary<string, string>
                {
                    ["run.googleapis.com/manualInstanceCount"] = "0",
                    ["run.googleapis.com/scalingMode"] = "manual"
                },
                labels = new Dictionary<string, string> { ["cloud.googleapis.com/location"] = config.Region }
            },
            spec = new
            {
                instanceSplits = new[] { new { latestRevision = true, percent = 100 } },
                template = new
                {
                    metadata = new
                    {
                        annotations = new Dictionary<string, string>
                        {
                            ["run.googleapis.com/execution-environment"] = "gen2",
                            ["run.googleapis.com/cloudsql-instances"] = connectionName
                        }
                    },
                    spec = new
                    {
                        serviceAccountName = serviceAccount,
                        containers = new object[]
                        {
                            new
                            {
                                name = "worker", image,
                                resources = new { limits = new { cpu = "1", memory = "512Mi" } },
                                env = environment.Select(pair => (object)new { name = pair.Key, value = pair.Value })
                                    .Concat(new[]
                                    {
                                        SecretEnvironment("RLS_WORKER_ConnectionStrings__RLS", connectionSecret),
                                        SecretEnvironment("RLS_WORKER_Jobillico__Username", usernameSecret),
                                        SecretEnvironment("RLS_WORKER_Jobillico__Password", passwordSecret)
                                    }).ToArray()
                            }
                        }
                    }
                }
            }
        };
        var definitionFile = Path.Combine(artifactsDirectory, "worker-definition.json");
        await File.WriteAllTextAsync(definitionFile, JsonSerializer.Serialize(definition));
        await processRunner.Run("gcloud",
            ["beta", "run", "worker-pools", "replace", definitionFile, $"--project={config.ProjectId}",
                "--quiet"]);
        await processRunner.Run("gcloud",
            ["beta", "run", "worker-pools", "update", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--instances=1",
                "--quiet"]);
        var description = await processRunner.Run("gcloud",
            ["beta", "run", "worker-pools", "describe", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--format=json", "--quiet"], captureOutput: true);
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "worker.json"), description);
        return description;
    }

    private static object SecretEnvironment(string name, string reference)
    {
        var parts = reference.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var version) || version <= 0)
        {
            throw new ArgumentException("Supply a secret name and a numeric version.", nameof(reference));
        }
        return new { name, valueFrom = new { secretKeyRef = new { name = parts[0], key = parts[1] } } };
    }
}
