using DialDen.RLS.UI.Language;
using DialDen.RLS.UI.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.UI.DependencyInjection;

public static class UIServiceCollectionExtensions
{
    public static IServiceCollection AddRLSUI(this IServiceCollection services)
    {
        services.AddLocalization();
        services.AddScoped<LanguageState>();
        services.AddScoped<NotificationStreamClient>();
        services.AddScoped<NotificationService>();
        return services;
    }
}
