namespace DialDen.RLS.JobApplications;

public sealed record EducationComparisonResult(bool? Met, double Confidence, string? Evidence);
