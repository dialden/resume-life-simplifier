namespace DialDen.RLS.Postings;

public sealed class JobSitePublicationChangedConcurrentlyException(int postingId, string jobSiteId)
    : Exception($"The publication of posting {postingId} on job site '{jobSiteId}' changed concurrently.");
