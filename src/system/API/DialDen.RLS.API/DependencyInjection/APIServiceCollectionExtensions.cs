using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.API.DependencyInjection;

public static class APIServiceCollectionExtensions
{
    private const string French = "fr";
    private const string English = "en";

    public static IServiceCollection AddRLSAPI(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLocalization();
        return services;
    }

    /// <summary>Answers in the language of the Accept-Language header, in French when it asks for none we offer.</summary>
    public static IApplicationBuilder UseRLSLocalization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseRequestLocalization(options => options
            .SetDefaultCulture(French)
            .AddSupportedCultures(French, English)
            .AddSupportedUICultures(French, English));
    }
}
