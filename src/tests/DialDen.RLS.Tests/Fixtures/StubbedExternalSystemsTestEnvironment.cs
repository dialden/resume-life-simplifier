using DialDen.RLS.JobSites;
using DialDen.RLS.Tests.Core.Drivers.JobSites;
using DialDen.RLS.Tests.Core.Drivers.JobSites.Stub;
using DialDen.RLS.Tests.Core.Dsl.JobSites;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Environment where the RLS talks to stubbed external systems. Register their stub drivers here.</summary>
public class StubbedExternalSystemsTestEnvironment : TestEnvironment
{
    protected override string ConfigurationEnvironmentVariablesPrefix => "STUBBED_EXT_SYSTEMS_";

    protected override string JobSitePublicationQueue => "rls-job-site-publication-stubbed";

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<JobSitesStub>();
        services.AddTransient<IJobSitesDriver, JobSitesStubDriver>();
        services.AddScoped<JobSitesDsl>();
    }

    protected override void ConfigureWorkerServices(IServiceCollection services, IServiceProvider testServices)
    {
        var stub = testServices.GetRequiredService<JobSitesStub>();
        services.RemoveAll<IJobSite>();
        services.AddSingleton<IJobSite>(new StubJobSite("jobillico", "Jobillico", stub));
    }
}
