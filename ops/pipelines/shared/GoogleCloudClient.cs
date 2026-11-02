using System.Text.RegularExpressions;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;

namespace DialDen.RLS.Pipelines;

public sealed class GoogleCloudClient(ProcessRunner processRunner, GoogleCloudConfig config)
{
    public async Task<string> ResolveRemoteImage(string sourceImage, string version, RemoteRegistryConfig registry)
    {
        ImageVersion.Validate(version);
        const string prefix = "ghcr.io/";
        if (!sourceImage.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("The remote repository only supports ghcr.io images.", nameof(sourceImage));
        }
        var imagePath = $"{config.ProjectId}/{registry.Repository}/{sourceImage[prefix.Length..]}";
        var host = $"{config.Region}-docker.pkg.dev";
        var token = (await processRunner.Run("gcloud",
            ["auth", "print-access-token", "--quiet"], captureOutput: true)).Trim();
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://{host}/v2/{imagePath}/manifests/{version}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"oauth2accesstoken:{token}")));
        request.Headers.Accept.ParseAdd("application/vnd.docker.distribution.manifest.v2+json");
        request.Headers.Accept.ParseAdd("application/vnd.oci.image.manifest.v1+json");
        request.Headers.Accept.ParseAdd("application/vnd.docker.distribution.manifest.list.v2+json");
        request.Headers.Accept.ParseAdd("application/vnd.oci.image.index.v1+json");
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Could not read '{sourceImage}:{version}' through '{host}/{imagePath}': " +
                $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                "Check the image tag, the GHCR token's package access, and Artifact Registry permissions.");
        }
        var digest = response.Headers.TryGetValues("Docker-Content-Digest", out var values)
            ? values.Single()
            : throw new InvalidOperationException($"Registry returned no digest for '{sourceImage}:{version}'.");
        if (!Regex.IsMatch(digest, @"\Asha256:[a-f0-9]{64}\z"))
        {
            throw new InvalidOperationException($"Registry returned an invalid image digest for '{sourceImage}:{version}'.");
        }
        var resolved = $"{host}/{imagePath}@{digest}";
        PipelineLog.Write($"Verified image: {resolved}");
        return resolved;
    }

    public async Task EnsureRemoteRepository(RemoteRegistryConfig registry)
    {
        ValidateSecretName(registry.TokenSecret);
        if (!Regex.IsMatch(registry.Repository, @"\A[a-z][a-z0-9-]{0,62}\z") ||
            string.IsNullOrWhiteSpace(registry.Username))
        {
            throw new InvalidOperationException("Registry configuration requires a repository name and GitHub username.");
        }

        var version = await ResolveSecretVersion(registry.TokenSecret);
        var secretVersion = $"projects/{config.ProjectNumber}/secrets/{registry.TokenSecret}/versions/{version}";

        using var repositories = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["artifacts", "repositories", "list", $"--project={config.ProjectId}",
                $"--location={config.Region}", "--format=json", "--quiet"], captureOutput: true));
        var name = $"projects/{config.ProjectId}/locations/{config.Region}/repositories/{registry.Repository}";
        foreach (var repository in repositories.RootElement.EnumerateArray())
        {
            if (repository.GetProperty("name").GetString() != name)
            {
                continue;
            }
            if (repository.GetProperty("format").GetString() != "DOCKER" ||
                repository.GetProperty("mode").GetString() != "REMOTE_REPOSITORY" ||
                !repository.TryGetProperty("remoteRepositoryConfig", out var remote) ||
                !remote.TryGetProperty("commonRepository", out var upstream) ||
                upstream.GetProperty("uri").GetString()?.TrimEnd('/') != "https://ghcr.io")
            {
                throw new InvalidOperationException(
                    $"Repository '{registry.Repository}' already exists but is not a Docker remote repository for GHCR.");
            }
            if (remote.TryGetProperty("upstreamCredentials", out var credentials) &&
                credentials.TryGetProperty("usernamePasswordCredentials", out var password) &&
                password.GetProperty("username").GetString() == registry.Username &&
                password.GetProperty("passwordSecretVersion").GetString() == secretVersion)
            {
                PipelineLog.Write($"Remote repository '{registry.Repository}' already uses token version {version}.");
                return;
            }
            await processRunner.Run("gcloud",
                ["artifacts", "repositories", "update", registry.Repository, $"--project={config.ProjectId}",
                    $"--location={config.Region}", $"--remote-username={registry.Username}",
                    $"--remote-password-secret-version={secretVersion}", "--quiet"]);
            return;
        }

        await processRunner.Run("gcloud",
            ["artifacts", "repositories", "create", registry.Repository, $"--project={config.ProjectId}",
                $"--location={config.Region}", "--repository-format=docker", "--mode=remote-repository",
                "--remote-docker-repo=https://ghcr.io", $"--remote-username={registry.Username}",
                $"--remote-password-secret-version={secretVersion}", "--quiet"]);
    }

    public Task<string> EnableApis(params string[] services) =>
        processRunner.Run("gcloud",
            ["services", "enable", .. services, $"--project={config.ProjectId}", "--quiet"]);

    public Task<string> EnsureServiceIdentity(string service) =>
        processRunner.Run("gcloud",
            ["beta", "services", "identity", "create", $"--service={service}",
                $"--project={config.ProjectId}", "--quiet"]);

    public Task<string> GrantSecretAccess(string secretName, string serviceAccount)
    {
        ValidateSecretName(secretName);
        return processRunner.Run("gcloud",
            ["secrets", "add-iam-policy-binding", secretName, $"--project={config.ProjectId}",
                $"--member=serviceAccount:{serviceAccount}", "--role=roles/secretmanager.secretAccessor",
                "--condition=None", "--quiet"]);
    }

    public async Task<string> ResolveSecretVersion(string secretName)
    {
        ValidateSecretName(secretName);
        var version = (await processRunner.Run("gcloud",
            ["secrets", "versions", "list", secretName, $"--project={config.ProjectId}",
                "--filter=state:ENABLED", "--sort-by=~createTime", "--limit=1", "--format=value(name)", "--quiet"],
            captureOutput: true)).Trim();
        if (!Regex.IsMatch(version, @"\A[0-9]+\z"))
        {
            throw new InvalidOperationException(
                $"Secret '{secretName}' has no enabled version. Supply the credential through Secret Manager before releasing.");
        }
        return version;
    }

    public async Task EnsureSecret(string secretName)
    {
        ValidateSecretName(secretName);

        var names = await processRunner.Run("gcloud",
            ["secrets", "list", $"--project={config.ProjectId}", "--format=value(name)", "--quiet"],
            captureOutput: true);
        if (names.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(name => name.Trim().Equals(secretName, StringComparison.Ordinal)))
        {
            PipelineLog.Write($"Secret '{secretName}' already exists; its values and permissions are unchanged.");
            return;
        }

        await processRunner.Run("gcloud",
            ["secrets", "create", secretName, $"--project={config.ProjectId}",
                "--replication-policy=user-managed", $"--locations={config.Region}", "--quiet"]);
        PipelineLog.Write($"Created secret '{secretName}' without a value.");
    }

    public async Task EnsureGeneratedPassword(string secretName)
    {
        await EnsureSecret(secretName);
        using var versions = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["secrets", "versions", "list", secretName, $"--project={config.ProjectId}",
                "--format=json", "--quiet"], captureOutput: true));
        if (versions.RootElement.GetArrayLength() > 0)
        {
            if (!versions.RootElement.EnumerateArray().Any(version =>
                version.GetProperty("state").GetString() == "ENABLED"))
            {
                throw new InvalidOperationException(
                    $"Password secret '{secretName}' has no enabled version. Restore access or rotate it explicitly; " +
                    "the release will not silently replace an existing password.");
            }
            PipelineLog.Write($"Password secret '{secretName}' already has an enabled version; unchanged.");
            return;
        }

        await AddSecretVersion(secretName, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        PipelineLog.Write($"Generated and stored a password for '{secretName}'.");
    }

    public async Task EnsureSecretValue(string secretName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A secret value must not be empty.", nameof(value));
        }

        await EnsureSecret(secretName);
        using var versions = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["secrets", "versions", "list", secretName, $"--project={config.ProjectId}",
                "--format=json", "--quiet"], captureOutput: true));
        if (versions.RootElement.GetArrayLength() > 0 &&
            string.Equals(await ReadSecret(secretName), value, StringComparison.Ordinal))
        {
            PipelineLog.Write($"Secret '{secretName}' already contains the required value; no version added.");
            return;
        }

        await AddSecretVersion(secretName, value);
        PipelineLog.Write($"Stored a new version of secret '{secretName}'.");
    }

    public async Task<string> ReadSecret(string secretName)
    {
        ValidateSecretName(secretName);
        var value = await processRunner.Run("gcloud",
            ["secrets", "versions", "access", "latest", $"--secret={secretName}",
                $"--project={config.ProjectId}", "--quiet"], captureOutput: true);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Secret '{secretName}' contains no value.");
        }
        return value;
    }

    public Task<string> AddSecretVersion(string secretName, string value)
    {
        ValidateSecretName(secretName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A secret value must not be empty.", nameof(value));
        }
        return processRunner.Run("gcloud",
            ["secrets", "versions", "add", secretName, $"--project={config.ProjectId}",
                "--data-file=-", "--quiet"], standardInput: value);
    }

    private static void ValidateSecretName(string secretName)
    {
        if (!Regex.IsMatch(secretName, @"\A[a-zA-Z0-9_-]{1,255}\z"))
        {
            throw new ArgumentException("Secret names must contain 1-255 letters, digits, underscores or hyphens.",
                nameof(secretName));
        }
    }

    // Pipeline stages never prompt for a login.
    public async Task VerifyAuthentication()
    {
        var account = (await processRunner.Run("gcloud",
            ["auth", "list", "--filter=status:ACTIVE", "--format=value(account)"], captureOutput: true)).Trim();
        if (account.Length == 0)
        {
            throw new InvalidOperationException(
                "The Google Cloud CLI is not authenticated. Run ops/dev/authenticate.cs first.");
        }

        var projectNumber = (await processRunner.Run("gcloud",
            ["projects", "describe", config.ProjectId, "--format=value(projectNumber)"],
            captureOutput: true)).Trim();
        if (projectNumber != config.ProjectNumber)
        {
            throw new InvalidOperationException(
                $"Google Cloud project '{config.ProjectId}' has number '{projectNumber}', " +
                $"expected '{config.ProjectNumber}'.");
        }
        PipelineLog.Write($"Authenticated to Google Cloud project {config.ProjectId} as {account}.");
    }
}
