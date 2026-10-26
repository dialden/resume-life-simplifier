#!/usr/bin/env -S dotnet run

using System.Diagnostics;

var databaseDirectory = Path.GetFullPath("ops/database");
if (!File.Exists(Path.Combine(databaseDirectory, "flyway.toml")))
{
    throw new FileNotFoundException(
        "Could not find ops/database/flyway.toml. Run this script from the repository root.");
}

RequireEnvironmentVariable("FLYWAY_USER");
RequireEnvironmentVariable("FLYWAY_PASSWORD");
var databases = new[]
{
    RequireEnvironmentVariable("RLS_DEV_DATABASE"),
    RequireEnvironmentVariable("RLS_TEST_DATABASE")
};

var host = Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

foreach (var database in databases)
{
    var startInfo = new ProcessStartInfo("flyway")
    {
        WorkingDirectory = databaseDirectory
    };
    startInfo.ArgumentList.Add("-configFiles=flyway.toml");
    startInfo.ArgumentList.Add(
        $"-url=jdbc:postgresql://{host}:{port}/{database}");
    startInfo.ArgumentList.Add("migrate");

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("Could not start Flyway.");
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"Flyway migration failed for database '{database}' with exit code {process.ExitCode}.");
    }
}

static string RequireEnvironmentVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Set the {name} environment variable.");
    }

    return value;
}
