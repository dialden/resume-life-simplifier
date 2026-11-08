namespace DialDen.RLS.Abstractions.Applications;

/// <summary>An application available for review.</summary>
public sealed record ApplicationResponse(int Id, string Name, string Email, string Source, string? ReferralJobSiteId);
