using System.Collections.Concurrent;
using DialDen.RLS.Tests.Core.Clients.API;
using DialDen.RLS.Tests.Core.Clients.API.Endpoints;
using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.API;
using DialDen.RLS.Tests.Core.Drivers.RLS.UI;
using DialDen.RLS.Tests.Core.Dsl;
using DialDen.RLS.Tests.Core.Dsl.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Fixtures;
public abstract class TestEnvironment
{
    private readonly IHttpHost _apiHost;
    private readonly IHttpHost _uiHost;
    private readonly InProcessWorkerHost? _workerHost;
    private readonly object _startLock = new();
    private RLSConfiguration? _configuration;
    private IServiceScope _scope = null!;
    private readonly ConcurrentBag<IServiceScope> _scenarioScopes = new();
    private bool disposedValue;

    /// <summary>Only available once the API host has been started by <see cref="InteractWithSystemThrough"/>.</summary>
    public RLSConfiguration Configuration =>
        _configuration ?? throw new InvalidOperationException("The API host has not been started.");

    public TestEnvironment()
    {
        var testsConfiguration = GetTestsConfiguration();
        var hosting = TestConfiguration.From(testsConfiguration).Hosting;
        if (hosting.Mode == HostingMode.InProcess)
        {
            // Each environment's worker consumes its own queue, so it only answers with its own external systems.
            var settings = new Dictionary<string, string?>
            {
                ["Messaging:JobSitePublicationQueue"] = JobSitePublicationQueue
            };
            _workerHost = new InProcessWorkerHost(
                ConfigurationEnvironmentVariablesPrefix,
                settings,
                services => ConfigureWorkerServices(services, _scope.ServiceProvider));
            _apiHost = new InProcessAPIHttpHost(ConfigurationEnvironmentVariablesPrefix, settings);
            _uiHost = new InProcessUIHttpHost(() => _apiHost.Url);
        }
        else
        {
            _apiHost = new ExternalHttpHost(hosting.APIUrl);
            _uiHost = new ExternalHttpHost(hosting.UIUrl);
        }
        InitializeTestsServiceProvider();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Interact with the system through the default channel (API).
    /// </summary>
    public UseCaseDsl InteractWithSystem()
    {
        return InteractWithSystemThrough(Channel.API);
    }

    public UseCaseDsl InteractWithSystemThrough(Channel channel)
    {
        StartHostsFor(channel);
        // One scope per scenario, so each scenario gets its own UseCaseContext (alias -> unique value map)
        var scenarioScope = _scope.ServiceProvider.CreateScope();
        _scenarioScopes.Add(scenarioScope);
        var sp = scenarioScope.ServiceProvider;
        IRLSDriver driver = channel switch
        {
            Channel.UI => sp.GetRequiredService<UIRLSDriver>(),
            Channel.API => sp.GetRequiredService<APIRLSDriver>(),
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
        //TODO: Replace all this manual creation with a more robust dependency injection library (autofac)
        var ctfUseCaseFactory = ActivatorUtilities.CreateInstance<UseCaseFactory<IRLSDriver>>(sp, driver);
        var externalSystems = ActivatorUtilities.CreateInstance<ExternalSystemsDsl>(sp);
        var rls = ActivatorUtilities.CreateInstance<RLSDsl>(sp, ctfUseCaseFactory);
        return ActivatorUtilities.CreateInstance<UseCaseDsl>(sp, rls, externalSystems);
    }

    /// <summary>
    /// Override to configure additional services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Override to configure from the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to configure.</param>
    protected virtual void ConfigureTests(IServiceProvider serviceProvider)
    {
    }

    /// <summary>
    /// Override to replace the services of the in-process worker, such as its external systems.
    /// </summary>
    /// <param name="services">The worker services to configure.</param>
    /// <param name="testServices">The tests' services, to share stubs with the worker.</param>
    protected virtual void ConfigureWorkerServices(IServiceCollection services, IServiceProvider testServices)
    {
    }

    protected abstract string ConfigurationEnvironmentVariablesPrefix { get; }

    protected abstract string JobSitePublicationQueue { get; }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var scenarioScope in _scenarioScopes) scenarioScope.Dispose();
                _scope.Dispose();
                _uiHost.Dispose();
                _apiHost.Dispose();
                _workerHost?.Dispose();
            }
            disposedValue = true;
        }
    }

    /// <summary>Starts only the hosts the channel needs. Idempotent and safe for parallel scenarios.</summary>
    private void StartHostsFor(Channel channel)
    {
        switch (channel)
        {
            case Channel.API:
                StartAPIHost();
                break;
            case Channel.UI: // The UI needs the API, so it starts after it.
                StartAPIHost();
                StartUIHost();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
        }
    }

    private void StartAPIHost()
    {
        lock (_startLock)
        {
            if (_configuration != null) return;
            // The worker registers the available job sites before the API offers them.
            _workerHost?.Start();
            _apiHost.Start();
            _configuration = CreateConfiguration(_apiHost.Url);
        }
    }

    private void StartUIHost()
    {
        lock (_startLock)
        {
            _uiHost.Start();
            _configuration!.SetUIUrl(_uiHost.Url);
        }
    }

    private void InitializeTestsServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureTestServicesInternal(services);
        var serviceProvider = services.BuildServiceProvider();
        _scope = serviceProvider.CreateScope();
        ConfigureTests(_scope.ServiceProvider);
    }

    protected IConfiguration GetTestsConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.Sources.Clear();
        configBuilder.AddEnvironmentVariables(prefix: "RLS_TEST_");
        configBuilder.AddCommandLine(Environment.GetCommandLineArgs());
        return configBuilder.Build();
    }

    private RLSConfiguration CreateConfiguration(string webServerUrl)
    {
        if (string.IsNullOrWhiteSpace(webServerUrl))
        {
            throw new InvalidOperationException("Web server URL is not configured.");
        }
        var configuration = new RLSConfiguration(webServerUrl);
        return configuration;
    }

    private void ConfigureTestServicesInternal(IServiceCollection services)
    {
        // Factories: resolved only after the host has been started explicitly
        services.AddSingleton(_ => Configuration);
        services
        .AddHttpClient()
        .ConfigureHttpClientDefaults(builder =>
            builder.ConfigureHttpClient((sp, httpClient) =>
                httpClient.BaseAddress = new Uri(sp.GetRequiredService<RLSConfiguration>().WebServerUrl)));
        // Created lazily so API-only runs never start Playwright
        services.AddSingleton<IPlaywright>(_ => Playwright.CreateAsync().GetAwaiter().GetResult());
        services.AddTransient<UIClient>();
        services.AddTransient<UIRLSDriver>();
        services.AddTransient<APIRLSDriver>();
        services.AddTransient<APIClient>();
        services.AddTransient<HealthEndpoint>();
        services.AddScoped<GoToRLS>();
        services.AddScoped<UseCaseContext>();
        ConfigureTestServices(services);
    }
}