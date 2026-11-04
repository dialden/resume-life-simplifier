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
    public async Task CanCreatePostingWithDescription(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .When().CreatePosting().WithDescription("Build tools & support customers <globally>.")
            .Then().ShouldSucceed()
                .And().Posting().HasDescription("Build tools & support customers <globally>.");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanViewPostingDescription(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithDescription("Build tools & support customers <globally>.")
            .When().ViewPosting()
            .Then().ShouldSucceed()
                .And().Posting().HasDescription("Build tools & support customers <globally>.");
    }

    [Trait("Category", "Acceptance")]
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CannotCreatePostingWithoutDescription(string? description)
    {
        await _testEnvironment.Scenario(Channel.API)
            .When().CreatePosting().WithDescription(description!)
            .Then().ShouldFail().FieldErrorMessage("description", "A posting description is required.");
    }

    [Trait("Category", "Acceptance")]
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CannotCreatePostingWithoutDescriptionThroughUI(string description)
    {
        await _testEnvironment.Scenario(Channel.UI)
            .When().CreatePosting().WithDescription(description)
            .Then().ShouldFail().ErrorMessage("A posting description is required.");
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
    public async Task CanSelectJobSitesWhenPublishing(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site")
            .Then().ShouldSucceed()
                .And().Posting().HasStatus("Published")
                    .HasJobSitePublication("stub-job-site", "Pending");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanSelectMultipleJobSitesWhenPublishing(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site", "another-stub-job-site")
            .Then().ShouldSucceed()
                .And().Posting().HasStatus("Published")
                    .HasJobSitePublicationCount(2)
                    .HasJobSitePublication("stub-job-site", "Pending")
                    .HasJobSitePublication("another-stub-job-site", "Pending");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task JobSitesRespondIndependentlyToTheSamePosting(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().JobSite("stub-job-site").AcceptsPostings()
            .And().JobSite("another-stub-job-site").RejectsPostings()
            .And().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site", "another-stub-job-site")
            .Then().ShouldSucceed()
                .And().Posting()
                    .HasJobSitePublicationCount(2)
                    .EventuallyHasJobSitePublication("stub-job-site", "Succeeded")
                    .EventuallyHasJobSitePublication("another-stub-job-site", "Failed");
    }

    [Trait("Category", "Acceptance")]
    [Theory]
    [InlineData(true, "Succeeded")]
    [InlineData(false, "Failed")]
    public async Task PublishedPostingUpdatesWithoutLeavingTheList(bool accepts, string outcome)
    {
        var given = _testEnvironment.Scenario(Channel.UI)
            .Given().Posting().WithCount(10).WithId("earlier")
            .And().JobSite("stub-job-site");
        if (accepts)
        {
            given.AcceptsPostings();
        }
        else
        {
            given.RejectsPostings();
        }
        await given.RespondsSlowly()
            .And().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site")
            .Then().ShouldSucceed()
                .And().Posting().HasJobSitePublication("stub-job-site", "Pending")
                    .UpdatesToJobSitePublication("stub-job-site", outcome);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ViewedPostingUpdatesWhenJobSitePublicationCompletes(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().JobSite("stub-job-site").AcceptsPostings().RespondsSlowly()
            .And().Posting().PublishedTo("stub-job-site")
            .When().ViewPosting()
            .Then().ShouldSucceed()
                .And().Posting()
                    .HasJobSitePublication("stub-job-site", "Pending")
                    .UpdatesToJobSitePublication("stub-job-site", "Succeeded");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanViewSucceededJobSitePublication(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedOnJobSite("stub-job-site")
            .When().ViewPosting()
            .Then().ShouldSucceed()
                .And().Posting().HasStatus("Published")
                    .HasJobSitePublication("stub-job-site", "Succeeded");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task CanObserveAnAlreadyCompletedPublication()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Posting().PublishedOnJobSite("stub-job-site")
            .When().ViewPosting()
            .Then().ShouldSucceed()
                .And().Posting().UpdatesToJobSitePublication("stub-job-site", "Succeeded");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task LiveUpdatesRemainAvailableAcrossNavigation()
    {
        await _testEnvironment.Scenario(Channel.UI)
            .Given().Posting().PublishedOnJobSite("stub-job-site")
            .When().ViewPosting()
            .Then().ShouldSucceed()
                .And().Posting().UpdatesToJobSitePublication("stub-job-site", "Succeeded");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task JobSitePublicationFailsWhenTheJobSiteRejectsThePosting(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().JobSite("stub-job-site").RejectsPostings()
            .And().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site")
            .Then().ShouldSucceed()
                .And().Posting().EventuallyHasJobSitePublication("stub-job-site", "Failed");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task JobSitePublicationSucceedsWhenTheJobSiteAcceptsThePosting(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().JobSite("stub-job-site").AcceptsPostings()
            .And().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site")
            .Then().ShouldSucceed()
                .And().Posting().EventuallyHasJobSitePublication("stub-job-site", "Succeeded");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanPublishWithoutExternalJobSites(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting()
            .When().PublishPosting()
            .Then().ShouldSucceed()
                .And().Posting().HasStatus("Published").HasNoJobSitePublications();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CannotPublishAnAlreadyPublishedPosting(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithId("opportunity").PublishedTo()
            .When().PublishPosting().WithId("opportunity")
            .Then().ShouldFail().ErrorMessage("This posting cannot be published again.");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CanListJobSitePublicationOutcome(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithId("opportunity").PublishedOnJobSite("stub-job-site")
            .When().ListPostings().AcrossAllPages()
            .Then().ShouldSucceed()
                .And().PostingPages().HasPostingJobSitePublication("opportunity", "stub-job-site", "Succeeded");
    }

    [Trait("Category", "Acceptance")]
    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("Stub-Job-Site")]
    public async Task CannotPublishToAnUnavailableJobSite(string jobSiteId)
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Posting()
            .When().PublishPosting().ToJobSites(jobSiteId)
            .Then().ShouldFail().FieldErrorMessage("jobSiteIds", "Select distinct, available job sites.");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task CannotSelectTheSameJobSiteTwice()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site", "stub-job-site")
            .Then().ShouldFail().FieldErrorMessage("jobSiteIds", "Select distinct, available job sites.");
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
