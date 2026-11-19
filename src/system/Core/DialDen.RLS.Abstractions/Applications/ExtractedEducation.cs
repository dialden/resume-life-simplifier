namespace DialDen.RLS.Abstractions.Applications;

public sealed record ExtractedEducation(
    string? Institution, string? Qualification, string? FieldOfStudy, string? StartDate, string? EndDate);
