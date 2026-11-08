namespace DialDen.RLS.Abstractions.Applications;

public interface IApplications
{
    Task<int?> Submit(SubmitApplicationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationResponse>?> List(int postingId, CancellationToken cancellationToken);
    Task<ApplicationResponse?> Find(int postingId, int applicationId, CancellationToken cancellationToken);
    Task<ResumeResponse?> Resume(int postingId, int applicationId, CancellationToken cancellationToken);
}
