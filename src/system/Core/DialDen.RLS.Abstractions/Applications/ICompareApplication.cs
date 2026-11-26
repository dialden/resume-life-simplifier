namespace DialDen.RLS.Abstractions.Applications;

public interface ICompareApplication
{
    Task<bool> Compare(int postingId, int applicationId, CancellationToken cancellationToken);
    Task<bool> RecordFailure(int postingId, int applicationId, CancellationToken cancellationToken);
}
