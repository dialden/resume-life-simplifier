namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record EducationAssessment(bool? OriginalMet, double Confidence, string? Evidence, bool? OverrideMet, string Note);
