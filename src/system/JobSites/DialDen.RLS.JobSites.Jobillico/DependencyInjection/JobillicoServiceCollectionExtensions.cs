using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.JobSites.Jobillico.DependencyInjection;

public static class JobillicoServiceCollectionExtensions
{
    public static IServiceCollection AddJobillico(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IJobSite, JobillicoJobSite>();

        return services;
    }
}
