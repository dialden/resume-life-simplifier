using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class JobPostingsTests
{
    private readonly TestEnvironments _testEnvironments;

    public JobPostingsTests(TestEnvironments testEnvironments)
    {
        _testEnvironments = testEnvironments;
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanCreatePosting(Channel channel, ExternalSystems externalSystem)
    {
        await _testEnvironments.For(externalSystem).Scenario(channel)
            .When().CreatePosting().WithTitle("Software Engineer")
            .Then().ShouldSucceed()
                .And().Posting().HasTitle("Software Engineer");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanViewCreatedPosting(Channel channel, ExternalSystems externalSystem)
    {
        await _testEnvironments.For(externalSystem).Scenario(channel)
            .Given().Posting()
            .When().ViewPosting()
            .Then().ShouldSucceed();
    }
}
