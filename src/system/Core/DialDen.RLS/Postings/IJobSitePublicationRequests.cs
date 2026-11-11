namespace DialDen.RLS.Postings;

public interface IJobSitePublicationRequests
{
    /// <summary>
    /// Requests publication on a job site. The request becomes durable only when
    /// <see cref="IPostingsArchive.Save"/> succeeds.
    /// </summary>
    Task Request(JobSitePublicationRequest request, CancellationToken cancellationToken);
}
