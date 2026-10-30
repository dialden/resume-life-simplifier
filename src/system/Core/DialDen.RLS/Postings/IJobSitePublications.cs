using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public interface IJobSitePublications
{
    /// <summary>
    /// Requests publishing the posting on each job site. The requests become durable only when
    /// <see cref="IPostingsArchive.Save"/> succeeds, so call this before saving the published posting.
    /// </summary>
    Task Request(Posting posting, IReadOnlyList<JobSite> jobSites, CancellationToken cancellationToken);

    Task<JobSitePublication?> Find(int postingId, string jobSiteId, CancellationToken cancellationToken);

    /// <summary>Saves the response of the job site to a pending publication.</summary>
    /// <exception cref="JobSitePublicationChangedConcurrentlyException">
    /// The job site's response was saved by someone else.
    /// </exception>
    Task Save(JobSitePublication publication, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobSitePublication>> ListFor(int postingId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, IReadOnlyList<JobSitePublication>>> ListFor(
        IReadOnlyList<int> postingIds, CancellationToken cancellationToken);
}
