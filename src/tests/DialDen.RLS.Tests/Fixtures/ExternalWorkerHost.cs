using System.Diagnostics;

namespace DialDen.RLS.Tests.Fixtures;

public sealed class ExternalWorkerHost(string containerName)
{
    public async Task Restart()
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new InvalidOperationException(
                "Hosting:WorkerContainer is required to restart the external Worker during resume analysis tests.");

        var startInfo = new ProcessStartInfo("docker")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("restart");
        startInfo.ArgumentList.Add("--time");
        startInfo.ArgumentList.Add("10");
        startInfo.ArgumentList.Add(containerName);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Docker to restart the external Worker.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var error = await errors;
        await output;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Could not restart the external Worker: {error}");
    }
}
