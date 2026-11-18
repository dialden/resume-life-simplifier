using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Core.Dsl;

public class UseCaseDsl : IAsyncDisposable
{
    private readonly Func<ValueTask<(UseCaseDsl Interaction, IAsyncDisposable Lifetime)>>? _createScope;
    private UseCaseDsl? _interaction;
    private IAsyncDisposable? _lifetime;
    private readonly RLSDsl? _rls;
    private readonly ExternalSystemsDsl? _externalSystems;
    private bool _executed;
    private bool _disposed;
    public RLSDsl RLS => _interaction?.RLS ?? _rls
        ?? throw new InvalidOperationException("The scenario has not started.");
    public ExternalSystemsDsl ExternalSystems => _interaction?.ExternalSystems ?? _externalSystems
        ?? throw new InvalidOperationException("The scenario has not started.");

    public UseCaseDsl(RLSDsl rls, ExternalSystemsDsl externalSystems, AsyncServiceScope scope)
    {
        _rls = rls;
        _externalSystems = externalSystems;
        _lifetime = scope;
    }

    internal UseCaseDsl(Func<ValueTask<(UseCaseDsl Interaction, IAsyncDisposable Lifetime)>> createScope)
    {
        _createScope = createScope;
    }

    internal async Task ExecuteAndDispose(Func<Task> execute)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_executed)
            throw new InvalidOperationException("An interaction can only be awaited once.");
        _executed = true;

        try
        {
            if (_createScope is not null)
                (_interaction, _lifetime) = await _createScope();
            await execute();
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, DisposeAsync);
            throw;
        }
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_lifetime is not null)
            await _lifetime.DisposeAsync();
    }
}
