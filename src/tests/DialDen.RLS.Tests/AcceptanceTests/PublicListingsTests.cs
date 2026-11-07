using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class PublicListingsTests
{
    private readonly TestEnvironment _testEnvironment;

    public PublicListingsTests(TestEnvironments testEnvironments)
    {
        _testEnvironment = testEnvironments.For(ExternalSystems.Stubbed);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task PublishingWithoutExternalDestinationsCreatesAShareablePublicListing(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithTitle("Software Engineer")
                .WithDescription("Build tools & support customers <globally>.\nWork with our team.")
            .When().PublishPosting()
            .Then().ShouldSucceed()
                .And().PublicListing()
                    .HasTitle("Software Engineer")
                    .HasDescription("Build tools & support customers <globally>.\nWork with our team.")
                    .HasStableShareableAddress();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CandidatesCanVisitAPublishedPostingDirectly(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithTitle("Customer Support Specialist")
                .WithDescription("Help customers succeed with our products.").PublishedTo()
            .When().ViewPublicListing()
            .Then().ShouldSucceed()
                .And().PublicListing()
                    .HasTitle("Customer Support Specialist")
                    .HasDescription("Help customers succeed with our products.")
                    .HasStableShareableAddress();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ExternalJobSitesReceiveTheSamePublicListingAddress(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().JobSite("stub-job-site").AcceptsPostings()
            .And().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site")
            .Then().ShouldSucceed()
                .And().PublicListing()
                    .HasStableShareableAddress()
                    .IsLinkedFromJobSite("stub-job-site");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task AnExternalPublicationFailureDoesNotRemoveThePublicListing(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().JobSite("stub-job-site").RejectsPostings()
            .And().Posting()
            .When().PublishPosting().ToJobSites("stub-job-site")
            .Then().ShouldSucceed()
                .And().PublicListing()
                    .RemainsAvailableAfterJobSiteRejects("stub-job-site")
                    .HasStableShareableAddress();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task DraftPostingsAreNotPubliclyAvailable(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting()
            .When().ViewPublicListing()
            .Then().ShouldFail().ErrorMessage("Public listing not found.");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnknownPostingsHaveNoPublicListing(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .When().ViewPublicListing().ForUnknownPosting()
            .Then().ShouldFail().ErrorMessage("Public listing not found.");
    }
}
