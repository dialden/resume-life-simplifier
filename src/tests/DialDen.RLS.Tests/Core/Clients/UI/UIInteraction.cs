using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public sealed class UIInteraction(UIClient client, AsyncServiceScope scope) : IAsyncDisposable
{
    public Task<HomePage> OpenHomePage() => client.OpenHomePage();

    public Task<string?> ReadLoadingLogoPathBeforeBlazorStarts() => client.ReadLoadingLogoPathBeforeBlazorStarts();

    public Task<string> ReadLoadingLogoAnimationNameBeforeBlazorStarts() => client.ReadLoadingLogoAnimationNameBeforeBlazorStarts();

    public Task<string> ReadLoadingScreenBackgroundColorBeforeBlazorStarts() => client.ReadLoadingScreenBackgroundColorBeforeBlazorStarts();

    public Task<string> ReadLoadingLogoFilterBeforeBlazorStarts() => client.ReadLoadingLogoFilterBeforeBlazorStarts();

    public Task<string> ReadLoadingPercentageBeforeBlazorStarts(int? percentage = null) =>
        client.ReadLoadingPercentageBeforeBlazorStarts(percentage);

    public ValueTask DisposeAsync() => scope.DisposeAsync();
}
