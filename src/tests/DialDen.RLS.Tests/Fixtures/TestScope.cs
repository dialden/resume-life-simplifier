using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Dsl;
using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Owns a scenario's interactions and environment, disposing interactions before the environment.</summary>
public sealed class TestScope : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly UIInteraction? _uiInteraction;
    private bool _disposed;

    public TestScope(TestEnvironment environment, UseCaseDsl interaction, ServiceProvider services,
        UIInteraction? uiInteraction)
    {
        Environment = environment;
        Interaction = interaction;
        _services = services;
        _uiInteraction = uiInteraction;
    }

    public TestEnvironment Environment { get; }
    public UseCaseDsl Interaction { get; }
    public UIInteraction UIInteraction => _uiInteraction
        ?? throw new InvalidOperationException("This scope does not use the UI channel.");

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await TestResourceCleanup.RunAsync(
            Interaction.DisposeAsync,
            _services.DisposeAsync,
            Environment.DisposeAsync);
    }
}
