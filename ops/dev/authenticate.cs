#!/usr/bin/env -S dotnet run

using System.Diagnostics;
using System.Text.Json;

const string RegistryScope = "write:packages";

var root = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root))
{
    throw new InvalidOperationException("Set WORKSPACE_ROOT to the absolute path of the repository root.");
}

using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "ops/pipelines/config.json")));
var registry = config.RootElement.GetProperty("images").GetProperty("api").GetString()!.Split('/')[0];
if (registry != "ghcr.io")
{
    throw new InvalidOperationException($"No authentication is implemented for registry '{registry}'.");
}

if (!await HasRegistryScope())
{
    Console.WriteLine($"GitHub CLI is not authenticated with the {RegistryScope} scope. Launching authentication...");
    await Run("gh", ["auth", "login", "--web", "--scopes", RegistryScope]);
}

var user = (await Run("gh", ["api", "user", "--jq", ".login"], captureOutput: true)).Trim();
var token = (await Run("gh", ["auth", "token"], captureOutput: true)).Trim();
await Run("docker", ["login", registry, "--username", user, "--password-stdin"], standardInput: token);
Console.WriteLine($"Authenticated to {registry} as {user}.");

async Task<bool> HasRegistryScope()
{
    var (exitCode, output) = await Execute("gh", ["auth", "status"], captureOutput: true);
    return exitCode == 0 && output.Contains($"'{RegistryScope}'", StringComparison.Ordinal);
}

async Task<string> Run(string command, string[] arguments, bool captureOutput = false, string? standardInput = null)
{
    var (exitCode, output) = await Execute(command, arguments, captureOutput, standardInput);
    return exitCode == 0
        ? output
        : throw new InvalidOperationException($"{command} {arguments[0]} failed with exit code {exitCode}.");
}

async Task<(int ExitCode, string Output)> Execute(string command, string[] arguments,
    bool captureOutput = false, string? standardInput = null)
{
    var startInfo = new ProcessStartInfo(command)
    {
        UseShellExecute = false,
        RedirectStandardOutput = captureOutput,
        RedirectStandardError = captureOutput,
        RedirectStandardInput = standardInput is not null
    };
    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Could not start {command}.");
    if (standardInput is not null)
    {
        await process.StandardInput.WriteAsync(standardInput);
        process.StandardInput.Close();
    }
    var outputTask = captureOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult("");
    var errorTask = captureOutput ? process.StandardError.ReadToEndAsync() : Task.FromResult("");
    await process.WaitForExitAsync();
    return (process.ExitCode, await outputTask + await errorTask);
}
