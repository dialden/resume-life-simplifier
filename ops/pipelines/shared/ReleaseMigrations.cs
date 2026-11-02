using System.Formats.Tar;

namespace DialDen.RLS.Pipelines;

public sealed class ReleaseMigrations(ProcessRunner processRunner)
{
    private const string SourceDirectory = "src/system/Database/migrations";

    public async Task<string> Export(string version, string artifactsDirectory)
    {
        ImageVersion.Validate(version);
        var commit = (await processRunner.Run("git",
            ["rev-parse", "--verify", "--end-of-options", $"{version}^{{commit}}"],
            captureOutput: true)).Trim();
        var destination = Path.Combine(artifactsDirectory, "release-source");
        Directory.CreateDirectory(destination);
        var archive = Path.Combine(artifactsDirectory, "migrations.tar");
        try
        {
            await processRunner.Run("git",
                ["archive", "--format=tar", $"--output={archive}", commit, "--", SourceDirectory]);
            TarFile.ExtractToDirectory(archive, destination, overwriteFiles: false);
        }
        finally
        {
            File.Delete(archive);
        }

        var migrations = Path.Combine(destination, SourceDirectory);
        if (!Directory.Exists(migrations) || !Directory.EnumerateFiles(migrations, "*.sql").Any())
        {
            throw new InvalidOperationException($"Release commit '{commit}' contains no SQL migrations in {SourceDirectory}.");
        }
        await File.WriteAllTextAsync(Path.Combine(artifactsDirectory, "migration-commit.txt"), commit + "\n");
        PipelineLog.Write($"Exported release migrations from commit {commit} to {migrations}.");
        return migrations;
    }
}
