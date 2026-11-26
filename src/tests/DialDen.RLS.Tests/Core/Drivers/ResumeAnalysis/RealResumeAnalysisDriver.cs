using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;

public sealed class RealResumeAnalysisDriver : IResumeAnalysisDriver
{
    public Task FailsAnalysis() =>
        throw new InvalidOperationException(
            "Controlling resume-analysis failures requires the stubbed external systems environment.");

    public Task ResetAndReturnNoContactsByDefault() => Task.CompletedTask;

    public Task FindsContactInformation(ExtractedContactInformation contacts, bool holdResponse) =>
        throw new InvalidOperationException(
            "Controlling resume-analysis responses requires the stubbed external systems environment.");

    public Task FindsEducation(
        ExtractedContactInformation contacts, IReadOnlyList<ExtractedEducation> education, bool holdResponse) =>
        throw new InvalidOperationException(
            "Controlling resume-analysis responses requires the stubbed external systems environment.");

    public Task ReturnsInvalidEducation(ExtractedContactInformation contacts) =>
        throw new InvalidOperationException(
            "Controlling resume-analysis responses requires the stubbed external systems environment.");

    public Task AssessesEducation(bool? met, double confidence, string? evidence) =>
        throw new InvalidOperationException(
            "Controlling education comparison responses requires the stubbed external systems environment.");

    public Task FailsEducationComparison() =>
        throw new InvalidOperationException(
            "Controlling education comparison responses requires the stubbed external systems environment.");

    public Task HoldEducationComparison() =>
        throw new InvalidOperationException(
            "Controlling education comparison responses requires the stubbed external systems environment.");
}
