using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.SmokeTests;

[Collection(RLSTestCollection.Name)]
public class RLSTests
{
    private readonly TestEnvironments _testEnvironments;

    public RLSTests(TestEnvironments testEnvironments)
    {
        _testEnvironments = testEnvironments;
    }

    [Trait("Category", "Smoke")]
    [Theory, CombinatorialData]
    public async Task ShouldBeUpAndRunning(Channel channel, ExternalSystems externalSystem)
    {
        await _testEnvironments.For(externalSystem).Scenario(channel)
            .Assume().RLS().ShouldBeRunning();
    }
}
