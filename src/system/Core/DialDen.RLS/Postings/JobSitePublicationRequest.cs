namespace DialDen.RLS.Postings;

public sealed record JobSitePublicationRequest(int PostingId, string JobSiteId, Uri PublicListingUrl);
