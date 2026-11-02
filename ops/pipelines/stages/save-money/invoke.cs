#!/usr/bin/env -S dotnet run

#:property PublishAot=false
#:project ../../shared/DialDen.RLS.Pipelines.csproj

using System.Text.Json;
using System.Text.RegularExpressions;

using DialDen.RLS.Pipelines;
using static DialDen.RLS.Pipelines.PipelineLog;

string? environment = null;
string? confirmation = null;
for (var index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--target-environment" when environment is null && index + 1 < args.Length:
            environment = args[++index];
            break;
        case "--confirm-delete" when confirmation is null && index + 1 < args.Length:
            confirmation = args[++index];
            break;
        default:
            throw new ArgumentException($"Unknown, duplicate, or incomplete argument: {args[index]}");
    }
}
if (environment is null || !Regex.IsMatch(environment, @"\A[a-z](?:[a-z0-9-]{0,18}[a-z0-9])?\z"))
{
    throw new ArgumentException("Supply --target-environment as 1-20 lowercase letters, digits or hyphens, " +
        "starting with a letter and not ending with a hyphen.");
}
if (confirmation != environment)
{
    throw new ArgumentException("Supply --confirm-delete with the exact target environment. " +
        "This permanently deletes its database and all data.");
}

var root = PipelineWorkspace.ResolveRoot();
var config = PipelineWorkspace.ReadConfig(root).GoogleCloud
    ?? throw new InvalidOperationException($"{PipelineWorkspace.ConfigFile} has no googleCloud section.");
config.Validate();
var artifacts = Path.Combine(root, ".artifacts", "save-money", environment,
    $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}");
Directory.CreateDirectory(artifacts);
var runner = new ProcessRunner(root, artifacts);
var prefix = $"rls-{environment}";
Write($"Permanently removing {prefix} UI, API, Worker and database from " +
    $"{config.ProjectId} ({config.Region}). Artifacts: {artifacts}");

// Read every resource inventory before deleting anything; failed reads must not mean "absent".
var services = await ReadNames(["run", "services"]);
var workers = await ReadNames(["beta", "run", "worker-pools"]);
using var instances = await ReadInstances();
var instanceExists = false;
foreach (var instance in instances.RootElement.EnumerateArray())
{
    if (instance.GetProperty("name").GetString() != prefix)
    {
        continue;
    }
    if (instance.GetProperty("region").GetString() != config.Region)
    {
        throw new InvalidOperationException($"Database '{prefix}' is outside configured region {config.Region}.");
    }
    instanceExists = true;
}

await DeleteRunResource(["run", "services"], $"{prefix}-ui", services);
await DeleteRunResource(["run", "services"], $"{prefix}-api", services);
await DeleteRunResource(["beta", "run", "worker-pools"], $"{prefix}-worker", workers);
if (instanceExists)
{
    await runner.Run("gcloud", ["sql", "instances", "patch", prefix,
        $"--project={config.ProjectId}", "--no-deletion-protection", "--quiet"]);
    await runner.Run("gcloud", ["sql", "instances", "delete", prefix,
        $"--project={config.ProjectId}", "--no-enable-final-backup", "--quiet"]);
}
else
{
    Write($"Database '{prefix}' is already absent.");
}

var remainingServices = await ReadNames(["run", "services"]);
var remainingWorkers = await ReadNames(["beta", "run", "worker-pools"]);
using var remainingInstances = await ReadInstances();
if (remainingServices.Contains($"{prefix}-ui") || remainingServices.Contains($"{prefix}-api") ||
    remainingWorkers.Contains($"{prefix}-worker") ||
    remainingInstances.RootElement.EnumerateArray().Any(instance =>
        instance.GetProperty("name").GetString() == prefix))
{
    throw new InvalidOperationException("Environment resources remain after deletion. Inspect the run and retry.");
}
Write($"Save money completed for '{environment}'. UI, API, Worker and database are absent. " +
    "Secrets, identities and shared registry are unchanged.");

async Task<HashSet<string>> ReadNames(string[] command)
{
    using var document = JsonDocument.Parse(await runner.Run("gcloud",
        [.. command, "list", $"--project={config.ProjectId}", $"--region={config.Region}",
            "--format=json", "--quiet"], captureOutput: true));
    return document.RootElement.EnumerateArray().Select(resource =>
        resource.GetProperty("metadata").GetProperty("name").GetString()
        ?? throw new InvalidOperationException("Resource inventory returned a missing name.")).ToHashSet();
}

async Task<JsonDocument> ReadInstances() => JsonDocument.Parse(await runner.Run("gcloud",
    ["sql", "instances", "list", $"--project={config.ProjectId}", "--format=json", "--quiet"],
    captureOutput: true));

async Task DeleteRunResource(string[] command, string name, HashSet<string> existing)
{
    if (!existing.Contains(name))
    {
        Write($"'{name}' is already absent.");
        return;
    }
    await runner.Run("gcloud", [.. command, "delete", name,
        $"--project={config.ProjectId}", $"--region={config.Region}", "--quiet"]);
}
