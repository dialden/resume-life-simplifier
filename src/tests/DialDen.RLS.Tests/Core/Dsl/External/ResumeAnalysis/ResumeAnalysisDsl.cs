using DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.External.ResumeAnalysis;

public sealed class ResumeAnalysisDsl(IResumeAnalysisDriver driver)
{
    public Task FailsAnalysis() => driver.FailsAnalysis();

    public Task Reset() => driver.ResetAndReturnNoContactsByDefault();

    public Task FindsContactInformation(ExtractedContactInformation contacts, bool holdResponse) =>
        driver.FindsContactInformation(contacts, holdResponse);

    public Task FindsEducation(
        ExtractedContactInformation contacts, IReadOnlyList<ExtractedEducation> education, bool holdResponse) =>
        driver.FindsEducation(contacts, education, holdResponse);

    public Task ReturnsInvalidEducation(ExtractedContactInformation contacts) =>
        driver.ReturnsInvalidEducation(contacts);

    public Task AssessesEducation(bool? met, double confidence, string? evidence) =>
        driver.AssessesEducation(met, confidence, evidence);
    public Task FailsEducationComparison() => driver.FailsEducationComparison();
    public Task HoldEducationComparison() => driver.HoldEducationComparison();
}
