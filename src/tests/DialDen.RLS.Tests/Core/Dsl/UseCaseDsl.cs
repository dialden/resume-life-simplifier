using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Core.Dsl;

public class UseCaseDsl : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private bool _executed;
    private bool _disposed;
    public RLSDsl RLS { get; }
    public ExternalSystemsDsl ExternalSystems { get; }

    public UseCaseDsl(RLSDsl rls, ExternalSystemsDsl externalSystems, AsyncServiceScope scope)
    {
        RLS = rls;
        ExternalSystems = externalSystems;
        _scope = scope;
    }

    internal async Task ExecuteAndDispose(Func<Task> execute)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_executed)
            throw new InvalidOperationException("An interaction can only be awaited once.");
        _executed = true;

        await using (this)
        {
            await execute();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _scope.DisposeAsync();
    }
}
