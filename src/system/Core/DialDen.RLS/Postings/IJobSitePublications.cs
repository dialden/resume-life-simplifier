using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public interface IJobSitePublications
{
    /// <summary>
    /// Requests publishing the posting on each job site. The requests become durable only when
    /// <see cref="IPostingsArchive.Save"/> succeeds, so call this before saving the published posting.
    /// </summary>
    Task Request(Posting posting, IReadOnlyList<JobSite> jobSites, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobSitePublication>> ListFor(int postingId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, IReadOnlyList<JobSitePublication>>> ListFor(
        IReadOnlyList<int> postingIds, CancellationToken cancellationToken);
}
