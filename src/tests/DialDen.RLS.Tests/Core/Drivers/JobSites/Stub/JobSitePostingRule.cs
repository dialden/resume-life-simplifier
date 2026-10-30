namespace DialDen.RLS.Tests.Core.Drivers.JobSites.Stub;

internal sealed record JobSitePostingRule(string JobSiteId, Func<int, bool> IsConcernedPosting, bool Accepts);
