using System.Diagnostics;

namespace DialDen.RLS.Pipelines;

public sealed class ProcessRunner(string workingDirectory, string artifactsDirectory)
{
    public async Task<string> Run(string command, string[] arguments,
        IReadOnlyDictionary<string, string>? environment = null, bool captureOutput = false)
    {
        Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss'Z'}] {command} {string.Join(" ", arguments)}");
        var startInfo = new ProcessStartInfo(command)
        {
            WorkingDirectory = workingDirectory,
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
                $"{command} {string.Join(" ", arguments)} failed with exit code {process.ExitCode}. " +
                $"{output} Artifacts: {artifactsDirectory}");
        }
        return output;
    }
}
