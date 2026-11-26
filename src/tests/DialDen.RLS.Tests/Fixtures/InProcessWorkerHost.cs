using DialDen.RLS.Worker.Root;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>The RLS worker hosted in the test process.</summary>
public sealed class InProcessWorkerHost : IWorkerHost
{
    private readonly object _startLock = new();
    private readonly string _environmentVariablesPrefix;
    private readonly IReadOnlyDictionary<string, string?> _settings;
    private readonly ScenarioDiagnostics _diagnostics;
    private IHost? _host;

    /// <param name="settings">Settings overriding the environment variables.</param>
    internal InProcessWorkerHost(
        string environmentVariablesPrefix,
        IReadOnlyDictionary<string, string?> settings,
        ScenarioDiagnostics diagnostics)
    {
        _environmentVariablesPrefix = environmentVariablesPrefix;
        _settings = settings;
        _diagnostics = diagnostics;
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
            builder.Logging.AddProvider(new ScenarioLogProvider(_diagnostics, "Worker"));
            builder.Logging.AddFilter<ScenarioLogProvider>(null, LogLevel.Warning);
            builder.Logging.AddFilter<ScenarioLogProvider>("MassTransit", LogLevel.None);
            _host = builder.Build();
            _host.Services.GetRequiredService<IBus>().ConnectConsumeObserver(new ScenarioConsumeObserver(_diagnostics));
            _host.StartAsync().GetAwaiter().GetResult();
        }
    }

    public void Dispose()
    {
        if (_host == null) return;
        var host = _host;
        _host = null;
        try
        {
            host.StopAsync().GetAwaiter().GetResult();
        }
        finally
        {
            host.Dispose();
        }
    }
}
