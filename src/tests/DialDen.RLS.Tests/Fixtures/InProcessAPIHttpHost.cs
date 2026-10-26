using DialDen.RLS.Tests.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>The RLS API hosted in the test process on a random Kestrel port.</summary>
public sealed class InProcessAPIHttpHost : WebApplicationFactory<Program>, IHttpHost
{
    private readonly object _startLock = new();
    private readonly string _environmentVariablesPrefix;
    private string? _url;

    public string Url => _url ?? throw new InvalidOperationException("The host has not been started.");

    public InProcessAPIHttpHost(string environmentVariablesPrefix)
    {
        _environmentVariablesPrefix = environmentVariablesPrefix;
    }

    /// <summary>Starts the host once. Subsequent calls are no-ops.</summary>
    public void Start()
    {
        lock (_startLock)
        {
            if (_url != null) return;
            // Configure the web host to use a random port
            UseKestrel(0);
            StartServer();
            _url = Services.GetRequiredService<IServer>().GetWebServerUrl();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration(configBuilder =>
        {
            var envSource = configBuilder.Sources.OfType<EnvironmentVariablesConfigurationSource>().Single();
            var priorityPrefix = _environmentVariablesPrefix + envSource.Prefix;
            configBuilder.AddEnvironmentVariables(priorityPrefix);
        });
    }
}
