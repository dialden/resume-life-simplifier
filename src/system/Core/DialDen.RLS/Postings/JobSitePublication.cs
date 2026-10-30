using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public sealed record JobSitePublication(int PostingId, string JobSiteId, JobSitePublicationStatus Status);
