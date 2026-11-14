namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record ExtractedEducation(
    string? Institution, string? Qualification, string? FieldOfStudy, string? StartDate, string? EndDate);
