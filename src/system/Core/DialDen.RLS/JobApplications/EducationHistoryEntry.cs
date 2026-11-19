using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public sealed class EducationHistoryEntry(
    int position, string? institution, string? qualification, string? fieldOfStudy, string? startDate, string? endDate)
{
    public int Position { get; private set; } = position;
    public string? Institution { get; private set; } = institution;
    public string? Qualification { get; private set; } = qualification;
    public string? FieldOfStudy { get; private set; } = fieldOfStudy;
    public string? StartDate { get; private set; } = startDate;
    public string? EndDate { get; private set; } = endDate;

    public ExtractedEducation ToExtractedEducation() =>
        new(Institution, Qualification, FieldOfStudy, StartDate, EndDate);
}
