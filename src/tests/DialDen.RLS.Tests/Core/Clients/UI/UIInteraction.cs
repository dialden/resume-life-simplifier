using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public sealed class UIInteraction(UIClient client, AsyncServiceScope scope) : IAsyncDisposable
{
    public Task<HomePage> OpenHomePage() => client.OpenHomePage();

    public ValueTask DisposeAsync() => scope.DisposeAsync();
}
