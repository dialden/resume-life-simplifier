#!/usr/bin/env -S dotnet run

#:property PublishAot=false
#:project ../../shared/DialDen.RLS.Pipelines.csproj

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using DialDen.RLS.Pipelines;

var (version, pushImages) = CommitStage.ParseArguments(args);
var stage = new CommitStage(version);

await stage.Build();
await stage.RunInProcessTests();
stage.ReportTestResults();
await stage.PublishApplications();
await stage.BuildImages();

if (pushImages)
{
    await stage.PushImages();
}

stage.ReportCompletion(pushImages);

public sealed class CommitStage
{
    private const string ConfigFile = PipelineWorkspace.ConfigFile;
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
        root = PipelineWorkspace.ResolveRoot();
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

    public Task RunInProcessTests() =>
        Run("dotnet",
            [
                "test", app.TestProject,
                "--configuration", "Release", "--no-build",
                "--filter", "Category=Acceptance|Category=Smoke",
                "--results-directory", results,
                "--logger", "console;verbosity=detailed",
                "--logger", "trx;LogFileName=commit.trx"
            ],
            new Dictionary<string, string> { ["RLS_TEST_Hosting__Mode"] = "InProcess" });

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
        IReadOnlyDictionary<string, string>? environment = null) =>
        processRunner.Run(command, arguments, environment);

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
        return (ImageVersion.Validate(version), push);
    }

    private PipelineConfig ReadConfig()
    {
        var config = PipelineWorkspace.ReadConfig(root);
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

    private static void RequireFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("An expected file is missing.", path);
        }
    }

    private static void Log(string message) =>
        PipelineLog.Write(message);
}
