using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public sealed record ApplicationSummary(
    int Id, string Name, string Email, string Source, string? ReferralJobSiteId,
    ApplicationStatus Status,
    ExtractedContactInformation? ExtractedContacts,
    IReadOnlyList<ExtractedEducation>? ExtractedEducation,
    ApplicationReviewGroup ReviewGroup,
    Abstractions.Applications.EducationAssessment? EducationAssessment,
    int ReviewVersion);
