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

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanListPostingsAcrossPages(Channel channel, ExternalSystems externalSystem)
    {
        await _testEnvironments.For(externalSystem).Scenario(channel)
            .Given().Posting().WithCount(11).WithId("opportunity")
            .When().ListPostings().AcrossAllPages()
            .Then().ShouldSucceed()
                .And().PostingPages().IncludesPostings("opportunity", 11);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task PageBeyondLastPostingIsEmpty(Channel channel, ExternalSystems externalSystem)
    {
        await _testEnvironments.For(externalSystem).Scenario(channel)
            .Given().Posting()
            .When().ListPostings().OnPage(int.MaxValue)
            .Then().ShouldSucceed()
                .And().PostingPages().HasEmptyPage(int.MaxValue);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CannotListNonPositivePage(
        Channel channel, ExternalSystems externalSystem, [CombinatorialValues(0, -1)] int pageNumber)
    {
        await _testEnvironments.For(externalSystem).Scenario(channel)
            .When().ListPostings().OnPage(pageNumber)
            .Then().ShouldFail();
    }
}
