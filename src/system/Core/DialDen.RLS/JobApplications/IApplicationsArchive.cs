namespace DialDen.RLS.JobApplications;

public interface IApplicationsArchive
{
    Task<int> NextId(CancellationToken cancellationToken);
    Task<int> Add(Application application, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationSummary>> List(int postingId, CancellationToken cancellationToken);
    Task<Application?> Find(int postingId, int applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves changes to a tracked application. Returns false if a concurrent update prevented the save.
    /// </summary>
    Task<bool> Save(Application application, CancellationToken cancellationToken);
}
