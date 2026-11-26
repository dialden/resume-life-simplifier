namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record ApplicationResponse(
    int Id, string Name, string Email, string Source,
    string? ReferralJobSiteId = null, string? Status = null,
    ExtractedContactInformation? ExtractedContacts = null,
    IReadOnlyList<ExtractedEducation>? ExtractedEducation = null,
    string? ReviewGroup = null,
    EducationAssessment? EducationAssessment = null,
    int ReviewVersion = 0)
{
    public string? ExtractionStatus => Status switch
    {
        "AwaitingExtraction" => "Pending",
        "ExtractionFailed" => "Failed",
        "AwaitingComparison" or "ComparisonFailed" or "Ready" => "Completed",
        _ => Status
    };
}
