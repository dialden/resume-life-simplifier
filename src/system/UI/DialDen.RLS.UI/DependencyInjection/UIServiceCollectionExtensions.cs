using System.Globalization;
using DialDen.RLS.UI.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace DialDen.RLS.UI.DependencyInjection;

public static class UIServiceCollectionExtensions
{
    public static IServiceCollection AddRLSUI(this IServiceCollection services, Uri apiBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(apiBaseAddress);

        services.AddLocalization();
        services.AddScoped(_ => new HttpClient(new AcceptLanguageHandler(new HttpClientHandler()))
        {
            BaseAddress = apiBaseAddress
        });
        return services;
    }

    /// <summary>Displays the workspace in the language the customer chose, in French until then.</summary>
    public static async Task UseRLSLanguageAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var javaScript = services.GetRequiredService<IJSRuntime>();
        var chosen = await javaScript.InvokeAsync<string?>("localStorage.getItem", RLSLanguages.StorageKey);
        var culture = CultureInfo.GetCultureInfo(RLSLanguages.Resolve(chosen));
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
