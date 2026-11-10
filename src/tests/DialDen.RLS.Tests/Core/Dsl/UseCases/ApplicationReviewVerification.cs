using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.External.ResumeAnalysis;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ApplicationReviewVerification(
    VoidValue response, UseCaseContext context, IRLSDriver driver, int postingId, int applicationId,
    ResumeAnalysisDsl resumeAnalysis)
    : ResponseVerification<VoidValue>(response, context)
{
    public async Task HasAnalysisAttempts(int expected) =>
        (await resumeAnalysis.AnalysisAttempts()).ShouldBe(expected);

    public async Task UpdatesToFailedExtraction()
    {
        var result = await driver.ObserveViewedApplications(
            postingId,
            applications => applications.Any(application =>
                application.Id == applicationId && application.ExtractionStatus == "Failed"),
            TimeSpan.FromSeconds(30));
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);
        result.Value.Single(application => application.Id == applicationId).ExtractionStatus.ShouldBe("Failed");
    }

    public async Task UpdatesToExtractedEmail(string? expected)
    {
        var result = await driver.ObserveViewedApplications(
            postingId,
            applications => applications.Any(application =>
                application.Id == applicationId && application.ExtractionStatus == "Completed"),
            TimeSpan.FromSeconds(30));
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);
        var application = result.Value.Single(application => application.Id == applicationId);
        application.ExtractionStatus.ShouldBe("Completed");
        application.ExtractedContacts.ShouldNotBeNull().Email.ShouldBe(expected);
    }
}
