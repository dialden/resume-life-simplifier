using DialDen.RLS.API.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.API.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddRLSApi(this IServiceCollection services)
    {
        services.AddSingleton<SseConnectionManager>();
        return services;
    }
}
