namespace DialDen.RLS.JobApplications;

public interface IApplicationChanges
{
    Task Record(int postingId, int applicationId, CancellationToken cancellationToken);
}
