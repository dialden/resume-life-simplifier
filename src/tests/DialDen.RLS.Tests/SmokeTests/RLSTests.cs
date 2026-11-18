using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.SmokeTests;

public class RLSTests(TestHarness harness)
{

    [Trait("Category", "Smoke")]
    [Theory, CombinatorialData]
    public async Task ShouldBeUpAndRunning(Channel channel, ExternalSystems externalSystem)
    {
        await harness.For(externalSystem).Through(channel)
            .Assume().RLS().ShouldBeRunning();
    }
}
