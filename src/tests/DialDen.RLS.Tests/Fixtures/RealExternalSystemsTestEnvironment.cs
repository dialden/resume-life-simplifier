using DialDen.RLS.Tests.Core.Drivers.JobSites;
using DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Environment where the RLS talks to the real external systems. Register their drivers here.</summary>
public class RealExternalSystemsTestEnvironment : TestEnvironment
{
    protected override string ConfigurationEnvironmentVariablesPrefix => "REAL_EXT_SYSTEMS_";

    protected override string JobSitePublicationQueue => "rls-job-site-publication-real";

    protected override string ApplicationReceivedTopic => "rls-application-received-real";

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddTransient<IJobSitesDriver, RealJobSitesDriver>();
        services.AddTransient<IResumeAnalysisDriver, RealResumeAnalysisDriver>();
    }
}
