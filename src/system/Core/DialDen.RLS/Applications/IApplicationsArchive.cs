namespace DialDen.RLS.Applications;

public interface IApplicationsArchive
{
    Task<int> Add(Application application, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationSummary>> List(int postingId, CancellationToken cancellationToken);
    Task<Application?> Find(int postingId, int applicationId, CancellationToken cancellationToken);
}
