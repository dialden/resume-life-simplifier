using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.JobSites.Jobillico.DependencyInjection;

public static class JobillicoServiceCollectionExtensions
{
    public static IServiceCollection AddJobillicoDependencies(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        ArgumentNullException.ThrowIfNull(configuration);
        return services;
    }
}
