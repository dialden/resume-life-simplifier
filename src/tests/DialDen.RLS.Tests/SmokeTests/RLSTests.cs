using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.SmokeTests;

public class RLSTests(TestHarness harness)
{
    [Trait("Category", "Smoke")]
    [Trait("ExternalSystems", "Stubbed")]
    [Theory, CombinatorialData]
    public async Task ShouldBeUpAndRunningWithStubbedExternalSystems(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Assume().RLS().ShouldBeRunning();
    }

    [Trait("Category", "Smoke")]
    [Trait("ExternalSystems", "Real")]
    [Theory, CombinatorialData]
    public async Task ShouldBeUpAndRunningWithRealExternalSystems(Channel channel)
    {
        await harness.For(ExternalSystems.Real).Through(channel)
            .Assume().RLS().ShouldBeRunning();
    }
}
