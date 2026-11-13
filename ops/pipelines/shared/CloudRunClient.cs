using System.Text.Json;

namespace DialDen.RLS.Pipelines;

public sealed class CloudRunClient(ProcessRunner processRunner, GoogleCloudConfig config)
{
    public async Task<string> DeployOllama(string name, string serviceAccount, string workerServiceAccount,
        string artifactsDirectory)
    {
        const string startup = """
            set -eu
            OLLAMA_HOST=127.0.0.1:11434 ollama serve &
            server=$!
            trap 'kill "$server" 2>/dev/null || true' EXIT
            attempts=0
            until OLLAMA_HOST=127.0.0.1:11434 ollama list >/dev/null 2>&1; do
                if ! kill -0 "$server" 2>/dev/null; then
                    echo "Ollama exited before becoming ready" >&2
                    exit 1
                fi
                attempts=$((attempts + 1))
                if [ "$attempts" -ge 60 ]; then
                    echo "Ollama did not start within 60 seconds" >&2
                    exit 1
                fi
                sleep 1
            done
            OLLAMA_HOST=127.0.0.1:11434 ollama pull qwen2.5vl:7b
            kill "$server"
            wait "$server" || true
            trap - EXIT
            exec ollama serve
            """;
        var flagsFile = Path.Combine(artifactsDirectory, "ollama-flags.json");
        await File.WriteAllTextAsync(flagsFile, JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["--command"] = new[] { "/bin/sh" },
            ["--args"] = new[] { "-c", startup },
            ["--set-env-vars"] = new Dictionary<string, string>
            {
                ["OLLAMA_HOST"] = "0.0.0.0:8080",
                ["OLLAMA_NUM_PARALLEL"] = "1"
            }
        }));
        await processRunner.Run("gcloud",
            ["run", "deploy", name, $"--project={config.ProjectId}", $"--region={config.Region}",
                "--image=docker.io/ollama/ollama@sha256:910d0d8107b538fb0894e63163eb195de0cc7aefcbc666dc62d3b18f71124592",
                $"--service-account={serviceAccount}", $"--flags-file={flagsFile}",
                "--execution-environment=gen2", "--port=8080", "--cpu=4", "--memory=16Gi",
                "--gpu=1", "--gpu-type=nvidia-l4", "--no-gpu-zonal-redundancy",
                "--no-cpu-throttling", "--no-use-http2", "--min-instances=0", "--max-instances=1",
                "--concurrency=1", "--timeout=600", "--ingress=internal",
                "--no-allow-unauthenticated", "--invoker-iam-check", "--deploy-health-check",
                "--startup-probe=tcpSocket.port=8080,periodSeconds=10,timeoutSeconds=1,failureThreshold=180",
                "--quiet"]);
        await processRunner.Run("gcloud",
            ["run", "services", "add-iam-policy-binding", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", $"--member=serviceAccount:{workerServiceAccount}",
                "--role=roles/run.invoker", "--condition=None", "--quiet"]);

        var description = await processRunner.Run("gcloud",
            ["run", "services", "describe", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--format=json", "--quiet"], captureOutput: true);
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "ollama.json"), description);
        using var service = JsonDocument.Parse(description);
        var url = service.RootElement.GetProperty("status").GetProperty("url").GetString();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttps || !address.Host.EndsWith(".run.app", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cloud Run returned no valid HTTPS run.app URL for Ollama '{name}'.");
        }
        PipelineLog.Write($"Private Ollama service deployed: {url}");
        return url;
    }

    public async Task<string> DeployUi(string name, string image, string serviceAccount,
        string apiName, string apiUrl, string artifactsDirectory)
    {
        var existingService = await ServiceExists(name);
        var environmentFile = Path.Combine(artifactsDirectory, "ui-environment.json");
        await File.WriteAllTextAsync(environmentFile,
            JsonSerializer.Serialize(new Dictionary<string, string> { ["API_BASE_URL"] = apiUrl }));
        await processRunner.Run("gcloud",
            ["run", "deploy", name, $"--project={config.ProjectId}", $"--region={config.Region}",
                $"--image={image}", $"--service-account={serviceAccount}",
                "--execution-environment=gen2", "--port=80", "--cpu=1", "--memory=512Mi",
                "--min-instances=0", "--max-instances=1", "--concurrency=80",
                "--ingress=all", "--allow-unauthenticated",
                .. (existingService ? new[] { "--no-traffic" } : Array.Empty<string>()),
                $"--env-vars-file={environmentFile}",
                "--clear-secrets", "--clear-cloudsql-instances",
                "--startup-probe=httpGet.path=/,httpGet.port=80,periodSeconds=5,timeoutSeconds=3,failureThreshold=24",
                "--quiet"]);
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
                $"--region={config.Region}",
                $"--update-env-vars=RLS_API_Cors__AllowedOrigins__0={url},RLS_API_PublicWebsite__BaseUrl={url}",
                "--quiet"]);
        await processRunner.Run("gcloud",
            ["run", "services", "update-traffic", apiName, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--to-latest", "--quiet"]);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        using var apiResponse = await client.GetAsync(new Uri(new Uri(apiUrl, UriKind.Absolute), "/health"));
        var apiBody = await apiResponse.Content.ReadAsStringAsync();
        if (!apiResponse.IsSuccessStatusCode || apiBody.Trim() != "Healthy")
        {
            throw new InvalidOperationException(
                $"API health check failed at '{apiUrl}': HTTP {(int)apiResponse.StatusCode}, {apiBody}.");
        }

        await processRunner.Run("gcloud",
            ["run", "services", "update-traffic", name, $"--project={config.ProjectId}",
                $"--region={config.Region}", "--to-latest", "--quiet"]);

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
        var existingService = await ServiceExists(name);
        var environmentFile = Path.Combine(artifactsDirectory, "api-environment.json");
        await File.WriteAllTextAsync(environmentFile, JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["RLS_API_URLS"] = "http://+:8080",
            ["RLS_API_Logging__LogLevel__Default"] = "Warning",
            ["RLS_API_Messaging__JobSitePublicationQueue"] = "rls-job-site-publication",
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true"
        }));
        await processRunner.Run("gcloud",
            ["run", "deploy", name, $"--project={config.ProjectId}", $"--region={config.Region}",
                $"--image={image}", $"--service-account={serviceAccount}",
                "--execution-environment=gen2", "--port=8080", "--cpu=1", "--memory=512Mi",
                "--min-instances=0", "--max-instances=1", "--concurrency=10", "--timeout=3600",
                "--ingress=all", "--allow-unauthenticated",
                .. (existingService ? new[] { "--no-traffic" } : Array.Empty<string>()),
                $"--set-cloudsql-instances={connectionName}", $"--env-vars-file={environmentFile}",
                $"--set-secrets=RLS_API_ConnectionStrings__RLS={connectionSecret}",
                "--startup-probe=httpGet.path=/health,httpGet.port=8080,periodSeconds=5,timeoutSeconds=3,failureThreshold=24",
                "--quiet"]);
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
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "api-url.txt"), url + "\n");
        PipelineLog.Write(existingService
            ? $"API deployed without traffic pending UI configuration: {url}"
            : $"API created with initial traffic; UI configuration follows: {url}");
        return url;
    }

    public async Task<string> DeployWorker(string name, string image, string connectionName,
        string serviceAccount, JobillicoConfig jobSite, string connectionSecret,
        string usernameSecret, string passwordSecret, string ollamaUrl, string artifactsDirectory)
    {
        jobSite.Validate();
        var environment = new Dictionary<string, string>
        {
            ["RLS_WORKER_Logging__LogLevel__Default"] = "Warning",
            ["RLS_WORKER_JobSites__Type"] = "real",
            ["RLS_WORKER_Jobillico__Endpoint"] = jobSite.Endpoint,
            ["RLS_WORKER_Jobillico__RecruiterId"] = jobSite.RecruiterId,
            ["RLS_WORKER_Jobillico__LocationId"] = jobSite.LocationId,
            ["RLS_WORKER_Jobillico__UseProduction"] = jobSite.UseProduction.ToString(),
            ["RLS_WORKER_Messaging__JobSitePublicationQueue"] = "rls-job-site-publication",
            ["RLS_WORKER_Ollama__Endpoint"] = ollamaUrl,
            ["RLS_WORKER_Ollama__NumCtx"] = "128000",
            ["RLS_WORKER_Ollama__IdentityTokenAudience"] = ollamaUrl
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
                            ["run.googleapis.com/cloudsql-instances"] = connectionName,
                            ["run.googleapis.com/network-interfaces"] = JsonSerializer.Serialize(new[]
                            {
                                new { network = GoogleCloudNetworkClient.Network, subnetwork = GoogleCloudNetworkClient.Subnet }
                            }),
                            ["run.googleapis.com/vpc-access-egress"] = "private-ranges-only"
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

    private async Task<bool> ServiceExists(string name)
    {
        using var services = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["run", "services", "list", $"--project={config.ProjectId}", $"--region={config.Region}",
                "--format=json", "--quiet"], captureOutput: true));
        return services.RootElement.EnumerateArray().Any(service =>
            service.GetProperty("metadata").GetProperty("name").GetString() == name);
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
