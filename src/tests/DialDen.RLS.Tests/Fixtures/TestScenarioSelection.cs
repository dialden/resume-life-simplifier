using DialDen.RLS.Tests.Core.Dsl;
using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Extensions;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>
/// Describes the requested mode and channel without acquiring resources.
/// The returned scenario will acquire its scope only when awaited.
/// </summary>
public sealed class TestScenarioSelection(Func<Channel, ValueTask<TestScope>> createScope)
{
    public ScenarioDsl Through(Channel channel)
    {
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        return new UseCaseDsl(run => RunInScopeAsync(channel, scope => run(scope.Interaction))).Scenario();
    }

    /// <summary>Opens a UI scenario that is cleaned up when the returned client proxy is disposed.</summary>
    public async ValueTask<ScopedUIClient> ThroughUI() => new(await createScope(Channel.UI));

    private async Task RunInScopeAsync(Channel channel, Func<TestScope, Task> run)
    {
        var scope = await createScope(channel);
        try
        {
            await run(scope);
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, scope.ReportDiagnosticsAsync, scope.DisposeAsync);
            throw;
        }
        try
        {
            await scope.DisposeAsync();
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, scope.ReportDiagnosticsAsync);
            throw;
        }
    }
}
