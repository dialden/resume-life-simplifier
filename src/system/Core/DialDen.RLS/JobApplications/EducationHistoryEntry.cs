using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public sealed class EducationHistoryEntry(
    int position, string? institution, string? qualification, string? startDate, string? endDate)
{
    public int Position { get; private set; } = position;
    public string? Institution { get; private set; } = institution;
    public string? Qualification { get; private set; } = qualification;
    public string? StartDate { get; private set; } = startDate;
    public string? EndDate { get; private set; } = endDate;

    public ExtractedEducation ToExtractedEducation() =>
        new(Institution, Qualification, StartDate, EndDate);
}
