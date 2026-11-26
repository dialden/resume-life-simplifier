using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;

public interface IResumeAnalysisDriver
{
    Task ResetAndReturnNoContactsByDefault();

    Task FindsContactInformation(ExtractedContactInformation contacts, bool holdResponse);

    Task FindsEducation(
        ExtractedContactInformation contacts, IReadOnlyList<ExtractedEducation> education, bool holdResponse);

    Task ReturnsInvalidEducation(ExtractedContactInformation contacts);

    Task AssessesEducation(bool? met, double confidence, string? evidence);
    Task FailsEducationComparison();
    Task HoldEducationComparison();

    Task FailsAnalysis();
}
