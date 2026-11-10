namespace DialDen.RLS.Abstractions.Applications;

public interface IExtractApplicationInformation
{
    /// <summary>Returns true when this call completes a pending extraction.</summary>
    Task<bool> Extract(int postingId, int applicationId, CancellationToken cancellationToken);
}
