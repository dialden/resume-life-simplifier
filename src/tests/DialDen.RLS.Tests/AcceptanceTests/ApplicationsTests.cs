using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.AcceptanceTests;

public class ApplicationsTests(TestHarness harness)
{
    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CandidatesWithoutAnEducationRequirementQualifyAfterExtraction(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyQualifies();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CandidatesAreGroupedByEducationAssessment(
        Channel channel,
        [CombinatorialValues(true, false)] bool met)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis()
                .FindsEducation(qualification: "Bachelor of Science")
                .AssessesEducation(met, 0.95, "Bachelor of Science")
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup(met ? "Qualified" : "Disqualified");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task InsufficientEducationRequiresReview(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsNoEducation()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("Verify");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnmetEducationRequirementIsDisqualifiedRegardlessOfConfidence(
        Channel channel, [CombinatorialValues(0.0, 0.1, 0.85)] double confidence)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "High school diploma")
                .AssessesEducation(false, confidence, null)
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyIsInReviewGroup("Disqualified");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UncertainEducationVerdictRequiresVerificationEvenWithHighConfidence(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "University studies")
                .AssessesEducation(null, 0.95, null)
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("Verify");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task RecruitersCanOverrideAnUnmetEducationAssessmentWithoutEvidence(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "High school diploma")
                .AssessesEducation(false, 0.1, null)
            .And().Application()
            .When().ReviewEducation().MarkMet().WithNote("Qualification verified")
            .Then().ShouldSucceed()
                .And().EducationReview().IsSaved("Qualified", true, "Qualification verified");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnsupportedPositiveEducationAssessmentFailsComparison(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "High school diploma")
                .AssessesEducation(true, 0.95, null)
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("ComparisonFailed");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task InventedEducationEvidenceFailsComparison(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "High school diploma")
                .AssessesEducation(true, 0.95, "Invented qualification")
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("ComparisonFailed");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task LowConfidenceEducationAssessmentRequiresReview(
        Channel channel, [CombinatorialValues(0.84, 0.85)] double confidence)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "Bachelor of Science")
                .AssessesEducation(true, confidence, "Bachelor of Science")
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyIsInReviewGroup(confidence < 0.85 ? "Verify" : "Qualified");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ComparisonFailuresDoNotHideExtractedInformation(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "Bachelor of Science")
                .EducationComparisonIsUnavailable()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("ComparisonFailed")
                    .EventuallyHasExtractedEducation((null, "Bachelor of Science", null, null));
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ExtractedCandidatesAwaitComparison(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "Bachelor of Science")
                .HasNotCompletedEducationComparison()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("AwaitingComparison");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CandidatesAwaitingExtractionHaveTheirOwnGroup(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().HasNotCompletedAnalysis()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("AwaitingExtraction");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task CandidatesWithFailedExtractionHaveTheirOwnGroup(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().IsUnavailable()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyIsInReviewGroup("ExtractionFailed");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task RecruitersCanSaveEducationReview(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "Bachelor of Science")
                .AssessesEducation(false, 0.95, "Bachelor of Science")
            .And().Application()
            .When().ReviewEducation().MarkMet().WithNote("Equivalent qualification")
            .Then().ShouldSucceed()
                .And().EducationReview().IsSaved("Qualified", true, "Equivalent qualification");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task RecruitersCanRestoreTheOriginalEducationAssessment(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "Bachelor of Science")
                .AssessesEducation(false, 0.95, "Bachelor of Science")
            .And().Application()
            .And().EducationReview().MarkMet()
            .When().ReviewEducation().RestoreOriginal().WithNote("Reviewed")
            .Then().ShouldSucceed()
                .And().EducationReview().IsSaved("Disqualified", null, "Reviewed");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task RecruitersCanCompareEducationEvidenceAcrossCandidates(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "Bachelor of Science")
                .AssessesEducation(true, 0.95, "Bachelor of Science")
            .And().Application().WithName("Alex Example").WithEmail("alex@example.com")
            .And().Application().WithName("Sam Example").WithEmail("sam@example.com")
            .When().CompareCandidates("Alex Example", "Sam Example")
            .Then().ShouldSucceed()
                .And().CandidateComparison().ShowsEducationEvidence("Bachelor's degree", "Bachelor of Science",
                    "Alex Example", "Sam Example");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnmetEducationAssessmentsHaveNoProof(
        Channel channel,
        [CombinatorialValues(null, "High school diploma", "Required education not found")] string? modelEvidence)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().WithRequiredEducation("Bachelor's degree").PublishedTo()
            .And().ResumeAnalysis().FindsEducation(qualification: "High school diploma")
                .AssessesEducation(false, 0.95, modelEvidence)
            .And().Application().WithName("Alex Example").WithEmail("alex@example.com")
            .And().Application().WithName("Sam Example").WithEmail("sam@example.com")
            .When().CompareCandidates("Alex Example", "Sam Example")
            .Then().ShouldSucceed()
                .And().CandidateComparison().ShowsEducationEvidence("Bachelor's degree", null,
                    "Alex Example", "Sam Example");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task RecruitersCanReviewContactInformationExtractedFromResumes(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis()
                .FindsName("Alex Resume")
                .FindsEmail("resume@example.com")
                .FindsPhoneNumber("+1 202-555-0142")
                .FindsPostalAddress("123 Example Street, Example City")
                .FindsLinkedInUrl("https://www.linkedin.com/in/example-candidate")
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyHasExtractedContactInformation(
                        name: "Alex Resume",
                        email: "resume@example.com",
                        phoneNumber: "+1 202-555-0142",
                        postalAddress: "123 Example Street, Example City",
                        linkedInUrl: "https://www.linkedin.com/in/example-candidate");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task RecruitersCanReviewEducationExtractedFromResumes(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis()
                .FindsEducation(
                    institution: "Example University", qualification: "Bachelor of Science\nComputer Science",
                    startDate: "2016", endDate: "2020")
                .FindsEducation(
                    institution: "Example Institute", qualification: "Security Certificate",
                    endDate: "June 2022")
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyHasExtractedEducation(
                        ("Example University", "Bachelor of Science\nComputer Science", "2016", "2020"),
                        ("Example Institute", "Security Certificate", null, "June 2022"));
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ResumeAnalysisCanCompleteWithoutFindingEducation(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().FindsNoEducation()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyHasExtractedEducation();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ContactInformationIsRetainedWhenEducationIsFound(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis()
                .FindsEmail("resume@example.com")
                .FindsEducation(institution: "Example University", qualification: "Bachelor of Science")
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyHasExtractedContactInformation(email: "resume@example.com");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task InvalidEducationDoesNotLeavePartialExtractionResults(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().FindsEmail("resume@example.com").ReturnsInvalidEducation()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyHasFailedExtractionWithoutExtractedInformation();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ReviewedApplicationsUpdateWhenResumeAnalysisCompletes(
        Channel channel, [CombinatorialValues(null, "resume@example.com")] string? extractedEmail)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().HasNotCompletedAnalysis()
            .And().Application()
            .When().ResumeAnalysisCompletes().FindsEmail(extractedEmail)
            .Then().ShouldSucceed()
                .And().ReviewedApplications().UpdatesToExtractedEmail(extractedEmail);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ReviewedApplicationsRecoverLiveUpdatesAfterAConnectionInterruption()
    {
        await harness.For(ExternalSystems.Stubbed).Through(Channel.UI)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().HasNotCompletedAnalysis()
            .And().Application()
            .When().ResumeAnalysisCompletes().AfterConnectionInterruption().FindsEmail("resume@example.com")
            .Then().ShouldSucceed()
                .And().ReviewedApplications().UpdatesToExtractedEmail("resume@example.com");
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ReviewedApplicationsUpdateWhenResumeAnalysisFails(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().HasNotCompletedAnalysis()
            .And().Application()
            .When().ResumeAnalysisCompletes().WithFailure()
            .Then().ShouldSucceed()
                .And().ReviewedApplications().UpdatesToFailedExtraction();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ApplicationsRemainAvailableWhenResumeAnalysisIsUnavailable(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().IsUnavailable()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyHasFailedExtraction()
                    .HasSubmittedResume();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnreadableResumesHaveFailedExtraction(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithUnreadableResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyHasFailedExtraction();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ResumesAboveTheExtractionPageLimitHaveFailedExtraction(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithResumePages(21)
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyHasFailedExtraction();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ResumesAtTheExtractionPageLimitCanBeAnalyzed(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithResumePages(20)
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").EventuallyHasExtractedContactInformation();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task NewlyReceivedApplicationsAreAwaitingInformationExtraction(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis().HasNotCompletedAnalysis()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .IsAwaitingInformationExtraction();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ResumeAnalysisCanCompleteWithoutFindingContactInformation(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .And().ResumeAnalysis()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting")
                    .EventuallyHasExtractedContactInformation(
                        name: null, email: null, phoneNumber: null,
                        postalAddress: null, linkedInUrl: null);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task ApplicationsAreAvailableForReviewOnTheCorrectPosting(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
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
        await harness.For(ExternalSystems.Stubbed).Through(channel)
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
        await harness.For(ExternalSystems.Stubbed).Through(channel)
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
        await harness.For(ExternalSystems.Stubbed).Through(channel)
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
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithWordResume()
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").HasSubmittedResume();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task AResumeAtTheSizeLimitCanBeSubmitted(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithResumeOfSize(5 * 1024 * 1024)
            .Then().ShouldSucceed()
                .And().ApplicationForPosting("posting").HasSubmittedResume();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task AResumeAboveTheSizeLimitIsRejected(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithResumeOfSize(5 * 1024 * 1024 + 1)
            .Then().ShouldFail();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task DraftPostingsDoNotAcceptApplications(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting()
            .When().ApplyToPosting().WithResume()
            .Then().ShouldFail();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task UnknownPostingsDoNotAcceptApplications(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .When().ApplyToPosting().WithId("0").WithResume()
            .Then().ShouldFail();
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task AnInvalidWordResumeIsRejected(Channel channel)
    {
        await harness.For(ExternalSystems.Stubbed).Through(channel)
            .Given().Posting().PublishedTo()
            .When().ApplyToPosting().WithInvalidWordResume()
            .Then().ShouldFail();
    }
}
