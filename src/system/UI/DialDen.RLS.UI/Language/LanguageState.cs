using System.Globalization;
using Microsoft.JSInterop;

namespace DialDen.RLS.UI.Language;

public class LanguageState
{
    public const string StorageKey = "rls-language";

    private readonly IJSRuntime _js;

    public LanguageState(IJSRuntime js) => _js = js;

    public event Action? LanguageChanged;

    public CultureInfo Current { get; private set; } = CultureInfo.CurrentUICulture;

    public async Task ChangeAsync(string language)
    {
        if (Current.Name == language)
        {
            return;
        }

        Current = new CultureInfo(language);
        Apply(Current);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, language);
        await _js.InvokeVoidAsync("rlsSetDocumentLanguage", language);
        LanguageChanged?.Invoke();
    }

    public static void Apply(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
