namespace DialDen.RLS.Abstractions.Postings;

public interface IPublishOnJobSite
{
    /// <summary>
    /// Publishes a posting on a job site it was requested on, then records whether the job site accepted it.
    /// Does nothing when the job site has already responded.
    /// </summary>
    Task Publish(PublishOnJobSiteRequest request, CancellationToken cancellationToken);
}
