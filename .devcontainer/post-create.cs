#!/usr/bin/env -S dotnet run

using System.Diagnostics;

var repositoryRoot = Directory.GetCurrentDirectory();
var completed = false;
try
{
    if (!File.Exists(Path.Combine(repositoryRoot, "ops/database/migrate.cs")))
    {
        throw new DirectoryNotFoundException(
            "Run this script from the repository root so it can find ops/database/migrate.cs.");
    }

    Console.WriteLine();
    Console.WriteLine("RLS development environment setup is running. Please wait before starting the debugger.");
    Console.WriteLine();

    await RunStepAsync(
        "[1/4] Migrating development and test databases...",
        "ops/database/migrate.cs");
    await RunStepAsync(
        "[2/4] Generating UI settings...",
        ".vscode/generate-ui-settings.cs");
    await RunStepAsync(
        "[3/4] Generating API and UI launch settings...",
        ".vscode/generate-launch-settings.cs");
    await RunStepAsync(
        "[4/4] Configuring the VS Code PostgreSQL connection...",
        ".vscode/configure-postgres-connection.cs");

    Console.WriteLine();
    Console.WriteLine("============================================================");
    Console.WriteLine("RLS setup complete. You can start working and debugging now.");
    Console.WriteLine("Select \"API + UI\" in Run and Debug, then press F5.");
    Console.WriteLine("============================================================");
    completed = true;
}
finally
{
    if (!completed)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("RLS setup failed. Fix the error above and rerun:");
        Console.Error.WriteLine("./.devcontainer/post-create.cs");
        Console.Error.WriteLine("Do not start debugging until setup succeeds.");
    }
}

async Task RunStepAsync(string message, string scriptPath)
{
    Console.WriteLine(message);

    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = repositoryRoot
    };
    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add(scriptPath);

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Could not start '{scriptPath}'.");
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"'{scriptPath}' failed with exit code {process.ExitCode}.");
    }
}
