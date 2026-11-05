using System.Globalization;
using DialDen.RLS.UI.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace DialDen.RLS.UI.DependencyInjection;

public static class UIServiceCollectionExtensions
{
    public static IServiceCollection AddRLSUI(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var apiBaseUrl = configuration["ApiBaseUrl"]
            ?? throw new InvalidOperationException("'ApiBaseUrl' is not configured in wwwroot/appsettings.json.");

        services.AddLocalization();
        services.AddScoped(_ => new HttpClient(new AcceptLanguageHandler(new HttpClientHandler()))
        {
            BaseAddress = new Uri(apiBaseUrl)
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
        await SetDocumentLanguageAsync(javaScript, culture);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static async Task SetDocumentLanguageAsync(IJSRuntime javaScript, CultureInfo culture)
    {
        await using var languageModule = await javaScript.InvokeAsync<IJSObjectReference>(
            "import", "./_content/DialDen.RLS.UI/language.js");
        await languageModule.InvokeVoidAsync("setDocumentLanguage", culture.TwoLetterISOLanguageName);
    }
}
