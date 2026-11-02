using System.Diagnostics;

namespace DialDen.RLS.Pipelines;

public sealed class ProcessRunner
{
    public ProcessRunner(string workingDirectory, string artifactsDirectory)
    {
        WorkingDirectory = workingDirectory;
        this.artifactsDirectory = artifactsDirectory;
    }

    private readonly string artifactsDirectory;
    public string WorkingDirectory { get; }

    public async Task<string> Run(string command, string[] arguments,
        IReadOnlyDictionary<string, string>? environment = null, bool captureOutput = false,
        string? standardInput = null, string? workingDirectory = null,
        IReadOnlyCollection<string>? removeEnvironmentPrefixes = null)
    {
        PipelineLog.Write($"{command} {string.Join(" ", arguments)}");
        var startInfo = new ProcessStartInfo(command)
        {
            WorkingDirectory = workingDirectory ?? WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = captureOutput,
            RedirectStandardInput = standardInput is not null
        };
        if (removeEnvironmentPrefixes is not null)
        {
            foreach (var name in startInfo.Environment.Keys
                .Where(name => removeEnvironmentPrefixes.Any(prefix =>
                    name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToArray())
            {
                startInfo.Environment.Remove(name);
            }
        }
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
        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            process.StandardInput.Close();
        }
        await process.WaitForExitAsync();
        var output = await outputTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{command} {string.Join(" ", arguments)} failed with exit code {process.ExitCode}. " +
                $"{output} Artifacts: {artifactsDirectory}");
        }
        return output;
    }
}
