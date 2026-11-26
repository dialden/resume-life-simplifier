namespace DialDen.RLS.Abstractions.Applications;

/// <summary>An application available for review.</summary>
public sealed record ApplicationResponse(
    int Id, string Name, string Email, string Source, string? ReferralJobSiteId,
    ApplicationStatus Status,
    ExtractedContactInformation? ExtractedContacts,
    IReadOnlyList<ExtractedEducation>? ExtractedEducation,
    ApplicationReviewGroup ReviewGroup,
    EducationAssessment? EducationAssessment,
    int ReviewVersion);
