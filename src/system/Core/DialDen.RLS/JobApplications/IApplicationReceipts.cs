namespace DialDen.RLS.JobApplications;

public interface IApplicationReceipts
{
    /// <summary>
    /// Announces receipt of an application. The announcement becomes durable only when
    /// <see cref="IApplicationsArchive.Add"/> succeeds, so call this before adding the application.
    /// </summary>
    Task Record(ApplicationReceipt receipt, CancellationToken cancellationToken);
}
