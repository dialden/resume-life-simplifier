namespace DialDen.RLS.Abstractions.Applications;

public sealed record EducationAssessment(
    bool? OriginalMet, double Confidence, string? Evidence, bool? OverrideMet, string Note);
