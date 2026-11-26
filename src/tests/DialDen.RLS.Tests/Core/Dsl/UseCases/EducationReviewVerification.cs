using DialDen.RLS.Tests.Core.Drivers.RLS;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class EducationReviewVerification(
    VoidValue response, UseCaseContext context, IRLSDriver driver, int postingId, int applicationId)
    : ResponseVerification<VoidValue>(response, context)
{
    public async Task IsSaved(string group, bool? verdict, string note)
    {
        var result = await driver.ListApplications(postingId);
        result.IsSuccess.ShouldBeTrue(result.IsFailure ? result.Error.ToString() : null);
        var application = result.Value.Single(item => item.Id == applicationId);
        application.ReviewGroup.ShouldBe(group);
        var assessment = application.EducationAssessment.ShouldNotBeNull();
        assessment.OverrideMet.ShouldBe(verdict);
        assessment.Note.ShouldBe(note);
    }
}
