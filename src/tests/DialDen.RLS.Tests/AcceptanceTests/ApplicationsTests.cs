using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class ApplicationsTests
{
    private readonly TestEnvironment _testEnvironment;

    public ApplicationsTests(TestEnvironments testEnvironments)
    {
        _testEnvironment = testEnvironments.For(ExternalSystems.Stubbed);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ApplicationsAreAvailableForReviewOnTheCorrectPosting(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().WithId("opportunity").PublishedTo()
            .And().Posting().WithId("other-opportunity").PublishedTo()
            .When().ApplyToPosting().WithId("opportunity")
                .WithName("Alex Example")
                .WithEmail("alex@example.com")
                .WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("opportunity")
                    .HasName("Alex Example")
                    .HasEmail("alex@example.com")
                    .HasSource("Client website")
                    .HasSubmittedResume()
                    .IsNotListedForPosting("other-opportunity");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task RepeatedApplicationsAreRetainedSeparately(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedTo()
            .And().Application().WithEmail("alex@example.com")
            .When().ApplyToPosting().WithEmail("alex@example.com").WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .HasApplicationCountForEmail(2);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ApplicationsRetainTheReferringJobSite(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedOnJobSite("stub-job-site")
            .When().ApplyToPosting().ReferredBy("stub-job-site").WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .HasSource("Client website")
                    .WasReferredBy("stub-job-site");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnrecognizedReferralsAreNotAttributedToAJobSite(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().ReferredBy("stub-job-site").WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .HasSource("Client website")
                    .WasNotReferredByAJobSite();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ApplicationsCanIncludeAWordResume(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithWordResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").HasSubmittedResume();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task AResumeAtTheSizeLimitCanBeSubmitted(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithResumeOfSize(5 * 1024 * 1024)
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").HasSubmittedResume();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task AResumeAboveTheSizeLimitIsRejected(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithResumeOfSize(5 * 1024 * 1024 + 1)
            .Then().ShouldFail();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task DraftPostingsDoNotAcceptApplications(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldFail();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnknownPostingsDoNotAcceptApplications(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .When().ApplyToPosting().WithId("0").WithResume()
            .Then().ShouldFail();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task AnInvalidWordResumeIsRejected(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithInvalidWordResume()
            .Then().ShouldFail();
    }
}
