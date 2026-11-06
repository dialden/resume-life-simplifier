using Microsoft.AspNetCore.Components;

namespace DialDen.RLS.UI.Language;

public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    [Inject]
    private LanguageState Language { get; set; } = null!;

    protected override void OnInitialized()
    {
        Language.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        _ = InvokeAsync(() =>
        {
            LanguageState.Apply(Language.Current);
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        Language.LanguageChanged -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }
}
