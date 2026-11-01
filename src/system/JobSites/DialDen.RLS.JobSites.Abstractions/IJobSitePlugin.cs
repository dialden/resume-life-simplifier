using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.JobSites;

/// <summary>Optional, parameterless plugin entry point for registering dependencies of discovered job sites.</summary>
public interface IJobSitePlugin
{
    /// <summary>Registers dependencies, such as named HTTP clients. The Worker registers IJobSite implementations.</summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
