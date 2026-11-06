namespace DialDen.RLS.JobSites;

public sealed record JobSitePosting(int PostingId, string Title, string Description, Uri? PublicListingUrl = null);
