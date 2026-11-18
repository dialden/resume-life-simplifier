using DialDen.RLS.Tests.Core.Clients;
using DialDen.RLS.Tests.Core.Clients.API;
using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Drivers.JobSites;
using DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.API;
using DialDen.RLS.Tests.Core.Drivers.RLS.UI;
using DialDen.RLS.Tests.Core.Dsl;
using DialDen.RLS.Tests.Core.Dsl.External.JobSites;
using DialDen.RLS.Tests.Core.Dsl.External.ResumeAnalysis;
using DialDen.RLS.Tests.Core.Dsl.UseCases;
using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>
/// Assembly-fixture entry point that owns shared resources and composes isolated test scopes.
/// A scenario acquires its environment only when its final assertion chain is awaited.
/// </summary>
public sealed class TestHarness : IAsyncLifetime
{
    private TestHarnessResources Resources { get; } = new();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        try
        {
            await Resources.InitializeAsync();
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, Resources.DisposeAsync);
            throw;
        }
    }

    public TestScenarioSelection For(ExternalSystems externalSystems)
    {
        if (!Enum.IsDefined(externalSystems))
            throw new ArgumentOutOfRangeException(nameof(externalSystems));
        return new(channel => CreateScopeAsync(externalSystems, channel));
    }

    private async ValueTask<TestScope> CreateScopeAsync(ExternalSystems externalSystems, Channel channel)
    {
        var database = await Resources.CreateDatabaseAsync();
        TestEnvironment? environment = null;
        ServiceProvider? services = null;
        AsyncServiceScope? interactionScope = null;
        WireMockMappings? jobSites = null;
        WireMockMappings? resumeAnalysis = null;
        try
        {
            environment = new(Resources, database.Name, database.ConnectionString, externalSystems, channel);
            environment.Start();
            if (externalSystems == ExternalSystems.Stubbed)
            {
                jobSites = new(Resources.JobSitesAdmin, environment.ExternalSystemsPath, Resources.JobSitesUrl);
                resumeAnalysis = new(Resources.ResumeAnalysisAdmin, environment.ExternalSystemsPath, Resources.ResumeAnalysisUrl);
            }
            services = CreateServices(environment, channel, jobSites, resumeAnalysis);
            interactionScope = services.CreateAsyncScope();
            var provider = interactionScope.Value.ServiceProvider;
            var interaction = new UseCaseDsl(
                provider.GetRequiredService<RLSDsl>(),
                provider.GetRequiredService<ExternalSystemsDsl>(),
                interactionScope.Value);
            var uiInteraction = channel == Channel.UI
                ? new UIInteraction(provider.GetRequiredService<UIClient>(), interactionScope.Value)
                : null;
            return new(environment, interaction, services, jobSites, resumeAnalysis, uiInteraction);
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error,
                () => interactionScope?.DisposeAsync() ?? ValueTask.CompletedTask,
                () => services?.DisposeAsync() ?? ValueTask.CompletedTask,
                () => environment is not null
                    ? environment.DisposeAsync()
                    : Resources.DropDatabaseAsync(database.Name),
                () => jobSites?.DisposeAsync() ?? ValueTask.CompletedTask,
                () => resumeAnalysis?.DisposeAsync() ?? ValueTask.CompletedTask);
            throw;
        }
    }

    ValueTask IAsyncDisposable.DisposeAsync() => Resources.DisposeAsync();

    private ServiceProvider CreateServices(
        TestEnvironment environment, Channel channel, WireMockMappings? jobSites, WireMockMappings? resumeAnalysis)
    {
        var services = new ServiceCollection();
        services.AddSingleton(environment.Configuration);
        services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = new Uri(environment.Configuration.WebServerUrl)
        });
        services.AddScoped<APIClient>();
        if (channel == Channel.UI)
        {
            services.AddScoped(_ => new UIClient(environment.Configuration, Resources.GetBrowserAsync));
            services.AddScoped<IRLSDriver, UIRLSDriver>();
        }
        else
        {
            services.AddScoped<IRLSDriver, APIRLSDriver>();
        }
        services.AddScoped<UseCaseContext>();
        services.AddScoped<GoToRLS>();
        services.AddScoped<UseCaseFactory<IRLSDriver>>();
        services.AddScoped<RLSDsl>();
        services.AddScoped<ExternalSystemsDsl>();
        services.AddScoped<JobSitesDsl>();
        services.AddScoped<ResumeAnalysisDsl>();
        if (jobSites is not null && resumeAnalysis is not null)
        {
            services.AddScoped<IJobSitesDriver>(_ => new WireMockJobSitesDriver(
                Resources.JobSitesAdmin, environment.JobSiteIds, jobSites));
            services.AddScoped<IResumeAnalysisDriver>(_ => new WireMockResumeAnalysisDriver(
                Resources.TestsConfiguration["ResumeAnalysis:CallbackHost"] ?? "localhost", resumeAnalysis));
        }
        else
        {
            services.AddScoped<IJobSitesDriver, RealJobSitesDriver>();
            services.AddScoped<IResumeAnalysisDriver, RealResumeAnalysisDriver>();
        }
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
