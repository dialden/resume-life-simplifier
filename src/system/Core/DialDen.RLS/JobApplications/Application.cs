namespace DialDen.RLS.JobApplications;

public sealed record Application(
    int Id, int PostingId, string Name, string Email, string ResumeFileName,
    byte[] Resume, string Source, string? ReferralJobSiteId);
