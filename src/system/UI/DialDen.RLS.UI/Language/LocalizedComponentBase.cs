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

    protected override bool ShouldRender()
    {
        LanguageState.Apply(Language.Current);
        return base.ShouldRender();
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
