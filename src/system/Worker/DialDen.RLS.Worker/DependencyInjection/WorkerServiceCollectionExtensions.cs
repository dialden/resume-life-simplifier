using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Worker.DependencyInjection;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddRLSWorker(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<JobSitesRegistration>();

        return services;
    }
}
