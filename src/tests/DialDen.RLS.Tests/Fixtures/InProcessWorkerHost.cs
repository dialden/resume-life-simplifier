using DialDen.RLS.Worker.Root;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>The RLS worker hosted in the test process.</summary>
public sealed class InProcessWorkerHost : IWorkerHost
{
    private readonly object _startLock = new();
    private readonly string _environmentVariablesPrefix;
    private readonly IReadOnlyDictionary<string, string?> _settings;
    private IHost? _host;

    /// <param name="settings">Settings overriding the environment variables.</param>
    public InProcessWorkerHost(
        string environmentVariablesPrefix,
        IReadOnlyDictionary<string, string?> settings)
    {
        _environmentVariablesPrefix = environmentVariablesPrefix;
        _settings = settings;
    }

    /// <summary>Starts the host once. Subsequent calls are no-ops.</summary>
    public void Start()
    {
        lock (_startLock)
        {
            if (_host != null) return;
            var builder = WorkerHost.CreateBuilder(["--environment", Environments.Development], configuration =>
                configuration
                    .AddEnvironmentVariables(_environmentVariablesPrefix + WorkerHost.EnvironmentVariablesPrefix)
                    .AddInMemoryCollection(_settings));
            var host = builder.Build();
            host.StartAsync().GetAwaiter().GetResult();
            _host = host;
        }
    }

    public void Dispose()
    {
        if (_host == null) return;
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
        _host = null;
    }
}
