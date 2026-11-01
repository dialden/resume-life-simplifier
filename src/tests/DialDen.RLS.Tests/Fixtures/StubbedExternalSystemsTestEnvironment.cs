using DialDen.RLS.Tests.Core.Drivers.JobSites;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestEase;
using WireMock.Client;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Environment where the RLS talks to stubbed external systems. Register their stub drivers here.</summary>
public class StubbedExternalSystemsTestEnvironment : TestEnvironment
{
    protected override string ConfigurationEnvironmentVariablesPrefix => "STUBBED_EXT_SYSTEMS_";

    protected override string JobSitePublicationQueue => "rls-job-site-publication-stubbed";

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        var wireMockUrl = GetTestsConfiguration()["JobSites:WireMockUrl"] ?? "http://wiremock-job-site/";
        if (!Uri.TryCreate(wireMockUrl, UriKind.Absolute, out var wireMockUri))
        {
            throw new InvalidOperationException($"JobSites:WireMockUrl is not a valid absolute URL: '{wireMockUrl}'.");
        }

        services.AddHttpClient("WireMockJobSiteAdmin", client => client.BaseAddress = wireMockUri);
        services.AddTransient<IWireMockAdminApi>(provider =>
        {
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("WireMockJobSiteAdmin");
            return new RestClient(client).For<IWireMockAdminApi>();
        });
        var workerConfiguration = new ConfigurationBuilder()
            .AddEnvironmentVariables(ConfigurationEnvironmentVariablesPrefix + "RLS_WORKER_")
            .Build();
        var siteIds = workerConfiguration.GetSection("JobSites:Sites").GetChildren()
            .Select(site => site["Id"] ?? throw new InvalidOperationException("Stubbed test job sites require an Id."))
            .ToArray();
        if (siteIds.Length == 0)
        {
            throw new InvalidOperationException("Configure STUBBED_EXT_SYSTEMS_RLS_WORKER_JobSites__Sites for acceptance tests.");
        }
        services.AddTransient<IJobSitesDriver>(provider =>
            new WireMockJobSitesDriver(provider.GetRequiredService<IWireMockAdminApi>(), siteIds));
    }

}
