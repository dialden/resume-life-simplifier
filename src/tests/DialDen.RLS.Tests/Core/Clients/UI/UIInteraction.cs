using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public sealed class UIInteraction(UIClient client, AsyncServiceScope scope) : IAsyncDisposable
{
    public Task<HomePage> OpenHomePage() => client.OpenHomePage();

    public Task<string> ReadPublicPageHeadingInLanguage(string path, string language) =>
        client.ReadPublicPageHeadingInLanguage(path, language);

    public Task<bool> ShowsWorkspaceNavigationAt(string path) => client.ShowsWorkspaceNavigationAt(path);

    public Task<string?> ReadLoadingLogoPathBeforeBlazorStarts() => client.ReadLoadingLogoPathBeforeBlazorStarts();

    public Task<string> ReadLoadingLogoAnimationNameBeforeBlazorStarts() => client.ReadLoadingLogoAnimationNameBeforeBlazorStarts();

    public Task<string> ReadLoadingScreenBackgroundColorBeforeBlazorStarts() => client.ReadLoadingScreenBackgroundColorBeforeBlazorStarts();

    public Task<string> ReadLoadingLogoFilterBeforeBlazorStarts() => client.ReadLoadingLogoFilterBeforeBlazorStarts();

    public Task<string> ReadLoadingPercentageBeforeBlazorStarts(int? percentage = null) =>
        client.ReadLoadingPercentageBeforeBlazorStarts(percentage);

    public ValueTask DisposeAsync() => scope.DisposeAsync();
}
