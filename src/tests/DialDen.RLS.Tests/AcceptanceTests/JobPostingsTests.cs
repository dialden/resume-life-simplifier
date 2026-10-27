using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class JobPostingsTests
{
    private readonly TestEnvironment _testEnvironment;

    public JobPostingsTests(TestEnvironments testEnvironments)
    {
        _testEnvironment = testEnvironments.For(ExternalSystems.Stubbed);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanCreatePosting(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .When().CreatePosting().WithTitle("Software Engineer")
            .Then().ShouldSucceed()
                .And().Posting()
                    .HasTitle("Software Engineer")
                    .HasStatus("Draft");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanPublishPostingFromPostingsList(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithId("opportunity")
            .When().PublishPosting().WithId("opportunity")
            .Then().ShouldSucceed()
                .And().Posting().HasStatus("Published");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanViewCreatedPosting(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting()
            .When().ViewPosting()
            .Then().ShouldSucceed();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanListPostingsAcrossPages(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithCount(11).WithId("opportunities")
            .When().ListPostings().AcrossAllPages()
            .Then().ShouldSucceed()
                .And().PostingPages()
                    .IncludesPostings("opportunities", 11)
                    .HasPostingStatus("opportunities-1", "Draft");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task PageBeyondLastPostingIsEmpty(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting()
            .When().ListPostings().OnPage(int.MaxValue)
            .Then().ShouldSucceed()
                .And().PostingPages().HasEmptyPage(int.MaxValue);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CannotListNonPositivePage(
        Channel channel, [CombinatorialValues(0, -1)] int pageNumber)
    {
        await _testEnvironment.Scenario(channel)
            .When().ListPostings().OnPage(pageNumber)
            .Then().ShouldFail();
    }
}
