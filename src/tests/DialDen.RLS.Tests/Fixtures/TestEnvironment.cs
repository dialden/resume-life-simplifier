using DialDen.RLS.Tests.Core.Clients.API;
using DialDen.RLS.Tests.Core.Clients.API.Endpoints;
using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.API;
using DialDen.RLS.Tests.Core.Drivers.RLS.UI;
using DialDen.RLS.Tests.Core.Dsl;
using DialDen.RLS.Tests.Core.Dsl.External.JobSites;
using DialDen.RLS.Tests.Core.Dsl.External.ResumeAnalysis;
using DialDen.RLS.Tests.Core.Dsl.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Fixtures;
public abstract class TestEnvironment : IAsyncDisposable
{
    private readonly IHttpHost _apiHost;
    private readonly IHttpHost _uiHost;
    private readonly InProcessWorkerHost? _workerHost;
    private readonly ExternalWorkerHost? _externalWorkerHost;
    private readonly object _startLock = new();
    private RLSConfiguration? _configuration;
    private ServiceProvider _serviceProvider = null!;
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
                ["Messaging:JobSitePublicationQueue"] = JobSitePublicationQueue,
                ["Messaging:ApplicationReceivedTopic"] = ApplicationReceivedTopic,
                ["Messaging:ResumeExtractionQueue"] = $"{ApplicationReceivedTopic}-extraction"
            };
            _workerHost = new InProcessWorkerHost(
                ConfigurationEnvironmentVariablesPrefix,
                settings);
            _apiHost = new InProcessAPIHttpHost(ConfigurationEnvironmentVariablesPrefix, settings);
            _uiHost = new InProcessUIHttpHost(() => _apiHost.Url);
        }
        else
        {
            _apiHost = new ExternalHttpHost(hosting.APIUrl);
            _uiHost = new ExternalHttpHost(hosting.UIUrl);
            _externalWorkerHost = new ExternalWorkerHost(hosting.WorkerContainer);
        }
        InitializeTestsServiceProvider();
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
        var scenarioScope = _serviceProvider.CreateAsyncScope();
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
        return new UseCaseDsl(rls, externalSystems, scenarioScope);
    }

    public UIInteraction InteractWithUI()
    {
        StartHostsFor(Channel.UI);
        var scenarioScope = _serviceProvider.CreateAsyncScope();
        return new UIInteraction(scenarioScope.ServiceProvider.GetRequiredService<UIClient>(), scenarioScope);
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

    protected abstract string ConfigurationEnvironmentVariablesPrefix { get; }

    protected abstract string JobSitePublicationQueue { get; }

    protected abstract string ApplicationReceivedTopic { get; }

    protected async Task RestartWorker()
    {
        if (_workerHost is null)
        {
            await _externalWorkerHost!.Restart();
            return;
        }
        _workerHost.Dispose();
        _workerHost.Start();
    }

    public async ValueTask DisposeAsync()
    {
        if (!disposedValue)
        {
            await _serviceProvider.DisposeAsync();
            _uiHost.Dispose();
            _apiHost.Dispose();
            _workerHost?.Dispose();
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
        _serviceProvider = services.BuildServiceProvider();
        ConfigureTests(_serviceProvider);
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
        services.AddScoped<JobSitesDsl>();
        services.AddScoped<ResumeAnalysisDsl>();
        ConfigureTestServices(services);
    }
}