namespace DialDen.RLS.Abstractions.Applications;

/// <summary>An application submitted to a public posting.</summary>
public sealed record SubmitApplicationRequest(
    int PostingId, string Name, string Email, string ResumeFileName, byte[] Resume, string? ReferralJobSiteId);
