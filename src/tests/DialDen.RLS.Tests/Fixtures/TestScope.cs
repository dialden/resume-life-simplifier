using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Dsl;
using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Owns a scenario's DI scope, root service provider, and environment.</summary>
public sealed class TestScope : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly AsyncServiceScope _interactionScope;
    private readonly UIClient? _uiClient;
    private bool _disposed;

    public TestScope(TestEnvironment environment, UseCaseDsl interaction, ServiceProvider services,
        AsyncServiceScope interactionScope, UIClient? uiClient)
    {
        Environment = environment;
        Interaction = interaction;
        _services = services;
        _interactionScope = interactionScope;
        _uiClient = uiClient;
    }

    public TestEnvironment Environment { get; }
    public UseCaseDsl Interaction { get; }
    internal IUIClient UIClient => _uiClient
        ?? throw new InvalidOperationException("This scope does not use the UI channel.");

    internal async ValueTask ReportDiagnosticsAsync()
    {
        if (_uiClient is not null)
            await _uiClient.RecordDiagnosticsAsync();
        Environment.Diagnostics.WriteToTestOutput();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await TestResourceCleanup.RunAsync(
            _interactionScope.DisposeAsync,
            _services.DisposeAsync,
            Environment.DisposeAsync);
    }
}
