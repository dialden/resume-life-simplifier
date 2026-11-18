using DialDen.RLS.Tests.Core.Clients.UI;
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

        return new UseCaseDsl(async () =>
        {
            var scope = await createScope(channel);
            return (scope.Interaction, scope);
        }).Scenario();
    }

    public async Task Through(Channel channel, Func<UIInteraction, Task> interact)
    {
        if (channel != Channel.UI)
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "UI inspection requires the UI channel.");
        var scope = await createScope(channel);
        try
        {
            await interact(scope.UIInteraction);
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, scope.DisposeAsync);
            throw;
        }
        await scope.DisposeAsync();
    }
}
