#!/usr/bin/env -S dotnet run

#:property PublishAot=false

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

const string ConfigFile = "ops/pipelines/config.json";
string[] uiImageFiles = ["nginx.conf", "configure-ui.sh"];

var (version, pushImages) = ParseArguments(args);
var root = ResolveWorkspaceRoot();
var config = ReadConfig();
var app = config.App;
var apiImage = $"{config.Images.Api}:{version}";
var uiImage = $"{config.Images.Ui}:{version}";

var artifacts = Path.Combine(root, ".artifacts", "commit",
    $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}");
var results = Path.Combine(artifacts, "test-results");
var apiOutput = Path.Combine(artifacts, "publish", "api");
var uiOutput = Path.Combine(artifacts, "publish", "ui");
var uiRoot = Path.Combine(uiOutput, "wwwroot");
Directory.CreateDirectory(results);
Log($"Starting commit stage. Artifacts: {artifacts}");

await Run("dotnet", ["build", app.Solution, "--configuration", "Release"]);
await PublishApplications();
await BuildImages();
await RunAcceptanceTests();
ReportTestResults();
if (pushImages)
{
    await PushImage(apiImage);
    await PushImage(uiImage);
}
Log($"Commit stage completed. Images: {apiImage}, {uiImage}. Pushed: {pushImages}");

async Task PushImage(string image)
{
    var source = (await Run("docker", ["image", "inspect", image,
        "--format", "{{index .Config.Labels \"org.opencontainers.image.source\"}}"],
        captureOutput: true)).Trim();
    if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri) ||
        sourceUri.Scheme is not ("http" or "https"))
    {
        throw new InvalidOperationException($"Image {image} must have a valid org.opencontainers.image.source label.");
    }

    await Run("docker", ["push", image]);
    // GHCR needs the source on the index as well as the image config when provenance is present.
    await Run("docker", ["buildx", "imagetools", "create",
        "--annotation", $"index:org.opencontainers.image.source={source}",
        "--tag", image, image]);
}

async Task PublishApplications()
{
    await Run("dotnet", ["publish", app.ApiProject,
        "--configuration", "Release", "--no-restore", "--output", apiOutput]);
    await Run("dotnet", ["publish", app.UiProject,
        "--configuration", "Release", "--no-restore", "--output", uiOutput]);

    // Local browser settings must not become environment-specific release artifacts.
    foreach (var suffix in new[] { "", ".br", ".gz" })
    {
        File.Delete(Path.Combine(uiRoot, $"appsettings.json{suffix}"));
    }
    RequireFile(Path.Combine(apiOutput, "DialDen.RLS.API.Root.dll"));
    RequireFile(Path.Combine(uiRoot, "index.html"));
}

async Task BuildImages()
{
    await Run("docker", ["build", "--tag", apiImage,
        "--file", Path.Combine(root, app.DockerDirectory, "api/Dockerfile"), apiOutput]);

    // The UI image's own files are build inputs, so they are only present in the context during the build.
    foreach (var file in uiImageFiles)
    {
        File.Copy(Path.Combine(root, app.DockerDirectory, "ui", file), Path.Combine(uiRoot, file));
    }
    try
    {
        await Run("docker", ["build", "--tag", uiImage,
            "--file", Path.Combine(root, app.DockerDirectory, "ui/Dockerfile"), uiRoot]);
    }
    finally
    {
        foreach (var file in uiImageFiles)
        {
            File.Delete(Path.Combine(uiRoot, file));
        }
    }
}

// The acceptance tests run against the built images, not hosts inside the test process.
async Task RunAcceptanceTests()
{
    var apiPort = FreePort();
    var uiPort = FreePort();
    var apiUrl = $"http://127.0.0.1:{apiPort}";
    var uiUrl = $"http://127.0.0.1:{uiPort}";
    var containers = new List<string>();
    try
    {
        containers.Add(await StartContainer(apiImage, 8080, apiPort, ApiEnvironment(uiUrl)));
        containers.Add(await StartContainer(uiImage, 80, uiPort,
            new Dictionary<string, string> { ["API_BASE_URL"] = apiUrl }));
        await WaitUntilReady(apiUrl + "/health");
        await WaitUntilReady(uiUrl + "/");

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
                ["RLS_TEST_Hosting__UIUrl"] = uiUrl
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
        foreach (var container in containers)
        {
            await Run("docker", ["rm", "--force", container]);
        }
    }
}

// The test-environment API settings (e.g. the test database connection) become the API container's own.
Dictionary<string, string> ApiEnvironment(string uiUrl)
{
    const string TestEnvironmentPrefix = "STUBBED_EXT_SYSTEMS_RLS_API_";
    var environment = new Dictionary<string, string> { ["RLS_API_Cors__AllowedOrigins__0"] = uiUrl };
    foreach (var name in Environment.GetEnvironmentVariables().Keys.Cast<string>()
        .Where(name => name.StartsWith(TestEnvironmentPrefix, StringComparison.Ordinal)))
    {
        environment["RLS_API_" + name[TestEnvironmentPrefix.Length..]] =
            Environment.GetEnvironmentVariable(name)!;
    }
    return environment;
}

async Task<string> StartContainer(string image, int containerPort, int hostPort,
    Dictionary<string, string> environment)
{
    var name = $"rls-commit-{Guid.NewGuid():N}";
    // Values travel through the process environment so secrets never appear in the logged command line.
    string[] environmentArguments = [.. environment.Keys.SelectMany(key => new[] { "--env", key })];
    await Run("docker", ["run", "--detach", "--name", name,
        "--publish", $"127.0.0.1:{hostPort}:{containerPort}",
        .. await DatabaseHostArguments(), .. environmentArguments, image], environment);
    return name;
}

// Containers started here cannot resolve the compose service name of the database, so pin its address.
async Task<string[]> DatabaseHostArguments()
{
    var host = Environment.GetEnvironmentVariable("PGHOST");
    if (string.IsNullOrWhiteSpace(host))
    {
        return [];
    }
    var addresses = await Dns.GetHostAddressesAsync(host);
    return ["--add-host", $"{host}:{addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork)}"];
}

void ReportTestResults()
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

async Task<string> Run(string command, string[] arguments,
    IReadOnlyDictionary<string, string>? environment = null, bool captureOutput = false)
{
    Log($"{command} {string.Join(" ", arguments)}");
    var startInfo = new ProcessStartInfo(command)
    {
        WorkingDirectory = root,
        UseShellExecute = false,
        RedirectStandardOutput = captureOutput
    };
    foreach (var (name, value) in environment ?? new Dictionary<string, string>())
    {
        startInfo.Environment[name] = value;
    }
    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Could not start {command}.");
    var outputTask = captureOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult("");
    await process.WaitForExitAsync();
    var output = await outputTask;
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"{command} {arguments[0]} failed with exit code {process.ExitCode}. {output} Artifacts: {artifacts}");
    }
    return output;
}

async Task WaitUntilReady(string url)
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

(string Version, bool PushImages) ParseArguments(string[] arguments)
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

string ResolveWorkspaceRoot()
{
    var workspaceRoot = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
    if (string.IsNullOrWhiteSpace(workspaceRoot) || !Path.IsPathFullyQualified(workspaceRoot))
    {
        throw new InvalidOperationException("Set WORKSPACE_ROOT to the absolute path of the repository root.");
    }
    RequireFile(Path.Combine(workspaceRoot, ConfigFile));
    return workspaceRoot;
}

PipelineConfig ReadConfig()
{
    var config = JsonSerializer.Deserialize<PipelineConfig>(
        File.ReadAllText(Path.Combine(root, ConfigFile)), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException($"{ConfigFile} is empty.");
    RequireFile(Path.Combine(root, config.App.Solution));
    foreach (var image in new[] { config.Images.Api, config.Images.Ui })
    {
        if (!Regex.IsMatch(image, @"\A[a-z0-9][a-z0-9.-]*(?::[0-9]+)?/[a-z0-9._/-]+\z") ||
            image.Contains("..", StringComparison.Ordinal) || image.EndsWith('/'))
        {
            throw new InvalidOperationException(
                $"Image '{image}' in {ConfigFile} must be a lowercase registry/repository name without a tag.");
        }
    }
    return config.Images.Api != config.Images.Ui
        ? config
        : throw new InvalidOperationException("API and UI image names must differ.");
}

static int FreePort()
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

static void RequireFile(string path)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException("An expected file is missing.", path);
    }
}

static void Log(string message) =>
    Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss'Z'}] {message}");

// Missing keys in config.json fail deserialization instead of surfacing later as nulls.
record PipelineConfig(AppConfig App, ImagesConfig Images);

record AppConfig(string Solution, string ApiProject, string UiProject, string TestProject, string DockerDirectory);

record ImagesConfig(string Api, string Ui);
