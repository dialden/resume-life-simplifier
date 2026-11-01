using DialDen.RLS.JobSites.Jobillico.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.JobSites.Jobillico;

public sealed class JobillicoPlugin : IJobSitePlugin
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddJobillicoDependencies(configuration);
}
