namespace DialDen.RLS.Applications;

public sealed record ApplicationSummary(
    int Id, string Name, string Email, string Source, string? ReferralJobSiteId);
