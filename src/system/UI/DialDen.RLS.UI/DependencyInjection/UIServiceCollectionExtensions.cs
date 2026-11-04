using DialDen.RLS.UI.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.UI.DependencyInjection;

public static class UIServiceCollectionExtensions
{
    public static IServiceCollection AddRLSUI(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddScoped<NotificationStreamClient>();
        services.AddScoped<NotificationService>();
        return services;
    }
}
