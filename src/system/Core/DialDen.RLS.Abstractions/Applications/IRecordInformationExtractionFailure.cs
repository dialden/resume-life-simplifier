namespace DialDen.RLS.Abstractions.Applications;

public interface IRecordInformationExtractionFailure
{
    Task<bool> Record(int postingId, int applicationId, CancellationToken cancellationToken);
}
