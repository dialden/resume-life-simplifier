namespace DialDen.RLS.Abstractions.Applications;

/// <summary>Replaces the recruiter's education verdict override and note.</summary>
public sealed record ReviewEducationRequest(bool? OverrideMet, string? Note, int Version);
