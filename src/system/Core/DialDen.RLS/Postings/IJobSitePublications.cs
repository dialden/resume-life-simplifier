namespace DialDen.RLS.Postings;

public interface IJobSitePublications
{
    /// <summary>
    /// Stages a pending publication. It becomes durable only when
    /// <see cref="IPostingsArchive.Save"/> succeeds.
    /// </summary>
    Task Add(JobSitePublication publication, CancellationToken cancellationToken);

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
