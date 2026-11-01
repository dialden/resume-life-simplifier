#!/usr/bin/env -S dotnet run

#:property PublishAot=false
#:project ../../shared/DialDen.RLS.Pipelines.csproj

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using DialDen.RLS.Pipelines;

var (version, pushImages) = CommitStage.ParseArguments(args);
var stage = new CommitStage(version);

await stage.Build();
await stage.PublishApplications();
await stage.BuildImages();
await stage.RunAcceptanceTests();
stage.ReportTestResults();

if (pushImages)
{
    await stage.PushImages();
}

stage.ReportCompletion(pushImages);

public sealed class CommitStage
{
    private const string ConfigFile = "ops/pipelines/config.json";
    private readonly string[] uiImageFiles = ["nginx.conf", "configure-ui.sh"];
    private readonly string root;
    private readonly AppConfig app;
    private readonly string apiImage;
    private readonly string uiImage;
    private readonly string workerImage;
    private readonly string artifacts;
    private readonly string results;
    private readonly string apiOutput;
    private readonly string uiOutput;
    private readonly string workerOutput;
    private readonly string uiRoot;
    private readonly ProcessRunner processRunner;

    public CommitStage(string version)
    {
        root = ResolveWorkspaceRoot();
        var config = ReadConfig();
        app = config.App;
        apiImage = $"{config.Images.Api}:{version}";
        uiImage = $"{config.Images.Ui}:{version}";
        workerImage = $"{config.Images.Worker}:{version}";

        artifacts = Path.Combine(root, ".artifacts", "commit",
            $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}");
        processRunner = new ProcessRunner(root, artifacts);
        results = Path.Combine(artifacts, "test-results");
        apiOutput = Path.Combine(artifacts, "publish", "api");
        uiOutput = Path.Combine(artifacts, "publish", "ui");
        workerOutput = Path.Combine(artifacts, "publish", "worker");
        uiRoot = Path.Combine(uiOutput, "wwwroot");
        Directory.CreateDirectory(results);
        Log($"Starting commit stage. Artifacts: {artifacts}");
    }

    public Task Build() =>
        Run("dotnet", ["build", app.Solution, "--configuration", "Release"]);

    public async Task PushImages()
    {
        await Run("docker", ["push", apiImage]);
        await Run("docker", ["push", uiImage]);
        await Run("docker", ["push", workerImage]);
    }

    public void ReportCompletion(bool pushImages) =>
        Log($"Commit stage completed. Images: {apiImage}, {uiImage}, {workerImage}. Pushed: {pushImages}");

    public async Task PublishApplications()
    {
        await Run("dotnet", ["publish", app.ApiProject,
        "--configuration", "Release", "--no-restore", "--output", apiOutput]);
        await Run("dotnet", ["publish", app.UiProject,
        "--configuration", "Release", "--no-restore", "--output", uiOutput]);
        await Run("dotnet", ["publish", app.WorkerProject,
        "--configuration", "Release", "--no-restore", "--output", workerOutput]);

        // Local browser settings must not become environment-specific release artifacts.
        foreach (var suffix in new[] { "", ".br", ".gz" })
        {
            File.Delete(Path.Combine(uiRoot, $"appsettings.json{suffix}"));
        }
        RequireFile(Path.Combine(apiOutput, "DialDen.RLS.API.Root.dll"));
        RequireFile(Path.Combine(workerOutput, "DialDen.RLS.Worker.Root.dll"));
        RequireFile(Path.Combine(uiRoot, "index.html"));
    }

    public async Task BuildImages()
    {
        await BuildImage(apiImage, Path.Combine(root, app.DockerDirectory, "api/Dockerfile"), apiOutput);
        await BuildImage(workerImage, Path.Combine(root, app.DockerDirectory, "worker/Dockerfile"), workerOutput);

        // The UI image's own files are build inputs, so they are only present in the context during the build.
        foreach (var file in uiImageFiles)
        {
            File.Copy(Path.Combine(root, app.DockerDirectory, "ui", file), Path.Combine(uiRoot, file));
        }
        try
        {
            await BuildImage(uiImage, Path.Combine(root, app.DockerDirectory, "ui/Dockerfile"), uiRoot);
        }
        finally
        {
            foreach (var file in uiImageFiles)
            {
                File.Delete(Path.Combine(uiRoot, file));
            }
        }
    }

    // Docker's containerd image store adds provenance and SBOM attestations by default, which turns the image
    // into an index. GHCR denies the first push of a new package with GITHUB_TOKEN when it is an index, even
    // when annotated, so push a plain image manifest.
    // This omits build provenance and the software bill of materials (SBOM) attestations. Revisit these flags
    // when adding supply-chain verification or SBOM-based scanning; image scanning is still possible without them.
    private Task BuildImage(string image, string dockerfile, string context) =>
        Run("docker", ["build", "--provenance=false", "--sbom=false",
        "--tag", image, "--file", dockerfile, context]);

    // The acceptance tests run against the built images, not hosts inside the test process.
    public async Task RunAcceptanceTests()
    {
        var apiPort = FreePort();
        var uiPort = FreePort();
        var wireMockPort = FreePort();
        var apiUrl = $"http://127.0.0.1:{apiPort}";
        var uiUrl = $"http://127.0.0.1:{uiPort}";
        var wireMockUrl = $"http://127.0.0.1:{wireMockPort}";
        var network = $"rls-commit-{Guid.NewGuid():N}";
        var wireMockName = $"rls-wiremock-{Guid.NewGuid():N}";
        var workerName = $"rls-worker-{Guid.NewGuid():N}";
        var queue = $"rls-commit-publications-{Guid.NewGuid():N}";
        var containers = new List<string>();
        await Run("docker", ["network", "create", network]);
        try
        {
            await StartContainer("sheyenrath/wiremock.net", 80, wireMockPort,
                new Dictionary<string, string>(), network, containers, wireMockName);
            await WaitUntilReady(wireMockUrl + "/__admin/health");

            var workerEnvironment = ApplicationEnvironment("RLS_WORKER_");
            // Both processes use the API's guarded test database and the same isolated queue.
            var apiEnvironment = ApiEnvironment(uiUrl);
            var connectionString = apiEnvironment.GetValueOrDefault("RLS_API_ConnectionStrings__RLS")
                ?? throw new InvalidOperationException("STUBBED_EXT_SYSTEMS_RLS_API_ConnectionStrings__RLS is required.");
            workerEnvironment["RLS_WORKER_ConnectionStrings__RLS"] = connectionString;
            workerEnvironment["RLS_WORKER_Messaging__JobSitePublicationQueue"] = queue;
            workerEnvironment["RLS_WORKER_JobSites__Type"] = "stubbed";
            foreach (var key in workerEnvironment.Keys
                .Where(key => key.StartsWith("RLS_WORKER_JobSites__Sites__", StringComparison.Ordinal)
                    && key.EndsWith("__Url", StringComparison.Ordinal)).ToArray())
            {
                workerEnvironment[key] = $"http://{wireMockName}/";
            }
            apiEnvironment["RLS_API_Messaging__JobSitePublicationQueue"] = queue;

            await StartContainer(workerImage, null, null, workerEnvironment, network, containers, workerName);
            await StartContainer(apiImage, 8080, apiPort, apiEnvironment, network, containers);
            await StartContainer(uiImage, 80, uiPort,
                new Dictionary<string, string> { ["API_BASE_URL"] = apiUrl }, network, containers);
            await WaitUntilReady(apiUrl + "/health");
            await WaitUntilReady(uiUrl + "/");
            await WaitUntilJobSiteRegistered(apiUrl, workerName);

            await Run("dotnet",
                [
                    "test", app.TestProject,
                "--configuration", "Release", "--no-build",
                "--filter", "Category=Acceptance|Category=Smoke",
                "--results-directory", results,
                "--logger", "console;verbosity=detailed",
                "--logger", "trx;LogFileName=commit.trx"
                ],
                new Dictionary<string, string>
                {
                    ["RLS_TEST_Hosting__Mode"] = "External",
                    ["RLS_TEST_Hosting__APIUrl"] = apiUrl,
                    ["RLS_TEST_Hosting__UIUrl"] = uiUrl,
                    ["RLS_TEST_JobSites__WireMockUrl"] = wireMockUrl
                });
        }
        catch
        {
            // Container logs are only worth the noise when something went wrong.
            foreach (var container in containers)
            {
                await Run("docker", ["logs", container]);
            }
            throw;
        }
        finally
        {
            foreach (var container in containers.AsEnumerable().Reverse())
            {
                await Run("docker", ["rm", "--force", container]);
            }
            await Run("docker", ["network", "rm", network]);
        }
    }

    // The test-environment API settings (e.g. the test database connection) become the API container's own.
    private Dictionary<string, string> ApiEnvironment(string uiUrl)
    {
        var environment = ApplicationEnvironment("RLS_API_");
        environment["RLS_API_Cors__AllowedOrigins__0"] = uiUrl;
        return environment;
    }

    private Dictionary<string, string> ApplicationEnvironment(string applicationPrefix)
    {
        var testEnvironmentPrefix = "STUBBED_EXT_SYSTEMS_" + applicationPrefix;
        var environment = new Dictionary<string, string>();
        foreach (var name in Environment.GetEnvironmentVariables().Keys.Cast<string>()
            .Where(name => name.StartsWith(testEnvironmentPrefix, StringComparison.Ordinal)))
        {
            environment[applicationPrefix + name[testEnvironmentPrefix.Length..]] =
                Environment.GetEnvironmentVariable(name)!;
        }
        return environment;
    }

    private async Task StartContainer(string image, int? containerPort, int? hostPort,
        Dictionary<string, string> environment, string network, List<string> containers, string? containerName = null)
    {
        var name = containerName ?? $"rls-commit-{Guid.NewGuid():N}";
        // Values travel through the process environment so secrets never appear in the logged command line.
        string[] environmentArguments = [.. environment.Keys.SelectMany(key => new[] { "--env", key })];
        string[] portArguments = containerPort is not null && hostPort is not null
            ? ["--publish", $"127.0.0.1:{hostPort}:{containerPort}"]
            : [];
        await Run("docker", ["create", "--name", name,
        "--network", network, .. portArguments,
        .. await DatabaseHostArguments(), .. environmentArguments, image], environment);
        containers.Add(name);
        await Run("docker", ["start", name]);
    }

    private async Task WaitUntilJobSiteRegistered(string apiUrl, string workerName)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var running = await Run("docker", ["inspect", "--format", "{{.State.Running}}", workerName],
                captureOutput: true);
            if (running.Trim() != "true")
            {
                throw new InvalidOperationException("The acceptance-test Worker stopped before it was ready.");
            }

            using var response = await client.GetAsync(apiUrl + "/job-sites");
            response.EnsureSuccessStatusCode();
            using var sites = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (sites.RootElement.EnumerateArray().Any(site => site.GetProperty("id").GetString() == "stub-job-site"))
            {
                Log("Worker running; stub-job-site is available.");
                return;
            }
            await Task.Delay(500);
        }
        throw new TimeoutException("The acceptance-test Worker did not register stub-job-site within 30 seconds.");
    }

    // Containers started here cannot resolve the compose service name of the database, so pin its address.
    private async Task<string[]> DatabaseHostArguments()
    {
        var host = Environment.GetEnvironmentVariable("PGHOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return [];
        }
        var addresses = await Dns.GetHostAddressesAsync(host);
        return ["--add-host", $"{host}:{addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork)}"];
    }

    public void ReportTestResults()
    {
        XNamespace trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var counters = XDocument.Load(Path.Combine(results, "commit.trx"))
            .Root?.Element(trx + "ResultSummary")?.Element(trx + "Counters");
        if (!int.TryParse(counters?.Attribute("executed")?.Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out var executed) || executed == 0)
        {
            throw new InvalidOperationException("The commit test gate did not execute any tests.");
        }
        Log($"Tests: {executed} executed, {counters?.Attribute("passed")?.Value} passed, " +
            $"{counters?.Attribute("failed")?.Value} failed.");
    }

    private Task<string> Run(string command, string[] arguments,
        IReadOnlyDictionary<string, string>? environment = null, bool captureOutput = false) =>
        processRunner.Run(command, arguments, environment, captureOutput);

    private async Task WaitUntilReady(string url)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            try
            {
                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                Log($"Ready: {url}");
                return;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or TaskCanceledException && DateTime.UtcNow < deadline)
            {
                await Task.Delay(500);
            }
        }
    }

    public static (string Version, bool PushImages) ParseArguments(string[] arguments)
    {
        string? version = null;
        var push = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--push-images" when !push:
                    push = true;
                    break;
                case "--version" when version is null && index + 1 < arguments.Length:
                    version = arguments[++index];
                    break;
                default:
                    throw new ArgumentException($"Unknown, duplicate, or incomplete argument: {arguments[index]}");
            }
        }
        if (version is null || !Regex.IsMatch(version, @"\A\w[\w.-]{0,127}\z", RegexOptions.ECMAScript))
        {
            throw new ArgumentException("Supply --version as a valid Docker tag.");
        }
        return (version, push);
    }

    private static string ResolveWorkspaceRoot()
    {
        var workspaceRoot = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Path.IsPathFullyQualified(workspaceRoot))
        {
            throw new InvalidOperationException("Set WORKSPACE_ROOT to the absolute path of the repository root.");
        }
        RequireFile(Path.Combine(workspaceRoot, ConfigFile));
        return workspaceRoot;
    }

    private PipelineConfig ReadConfig()
    {
        var config = JsonSerializer.Deserialize<PipelineConfig>(
            File.ReadAllText(Path.Combine(root, ConfigFile)), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"{ConfigFile} is empty.");
        RequireFile(Path.Combine(root, config.App.Solution));
        var images = new[] { config.Images.Api, config.Images.Ui, config.Images.Worker };
        foreach (var image in images)
        {
            if (!Regex.IsMatch(image, @"\A[a-z0-9][a-z0-9.-]*(?::[0-9]+)?/[a-z0-9._/-]+\z") ||
                image.Contains("..", StringComparison.Ordinal) || image.EndsWith('/'))
            {
                throw new InvalidOperationException(
                    $"Image '{image}' in {ConfigFile} must be a lowercase registry/repository name without a tag.");
            }
        }
        return images.Distinct(StringComparer.Ordinal).Count() == images.Length
            ? config
            : throw new InvalidOperationException("API, UI and Worker image names must differ.");
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("An expected file is missing.", path);
        }
    }

    private static void Log(string message) =>
        Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss'Z'}] {message}");
}
