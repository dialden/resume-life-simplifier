namespace DialDen.RLS.Abstractions.Applications;

public interface IReviewApplicationEducation
{
    Task<bool> Review(int postingId, int applicationId, ReviewEducationRequest request, CancellationToken cancellationToken);
}
