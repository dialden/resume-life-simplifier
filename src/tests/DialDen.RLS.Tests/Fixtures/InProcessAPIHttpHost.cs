using DialDen.RLS.Tests.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>The RLS API hosted in the test process on a random Kestrel port.</summary>
public sealed class InProcessAPIHttpHost : IHttpHost
{
    private readonly IConfiguration _testsConfiguration;
    private readonly object _startLock = new();
    private WebApplicationFactory<Program>? _factory;
    private string? _url;

    public string Url => _url ?? throw new InvalidOperationException("The host has not been started.");

    public InProcessAPIHttpHost(IConfiguration testsConfiguration)
    {
        _testsConfiguration = testsConfiguration;
    }

    /// <summary>Starts the host once. Subsequent calls are no-ops.</summary>
    public void Start()
    {
        lock (_startLock)
        {
            if (_factory != null) return;
            var factory = new WebApplicationFactory<Program>();
            // Configure the web host to use a random port
            factory.UseKestrel(0);
            factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Development);
                builder.ConfigureAppConfiguration(ConfigureAppConfiguration);
            });

            factory.StartServer();
            _url = factory.Services.GetRequiredService<IServer>().GetWebServerUrl();
            _factory = factory;
        }
    }

    private void ConfigureAppConfiguration(IConfigurationBuilder configBuilder)
    {
        var testConnectionString = Environment.GetEnvironmentVariable("RLS_TEST_CONNECTION_STRING")
            ?? _testsConfiguration.GetConnectionString("RLS");
        if (string.IsNullOrWhiteSpace(testConnectionString))
        {
            throw new InvalidOperationException(
                "The test connection string must be configured in ConnectionStrings:RLS " +
                "or RLS_TEST_CONNECTION_STRING.");
        }

        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:RLS"] = testConnectionString
        });
    }

    public void Dispose() => _factory?.Dispose();
}
