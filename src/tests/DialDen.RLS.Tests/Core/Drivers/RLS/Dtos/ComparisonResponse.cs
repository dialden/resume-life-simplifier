namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record ComparisonResponse(string? EducationRequirement, IReadOnlyList<CandidateComparison> Candidates);
