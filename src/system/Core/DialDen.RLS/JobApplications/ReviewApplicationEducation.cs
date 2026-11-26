using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

internal sealed class ReviewApplicationEducation(IApplicationsArchive archive, IApplicationChanges changes)
    : IReviewApplicationEducation
{
    public async Task<bool> Review(
        int postingId, int applicationId, ReviewEducationRequest request, CancellationToken cancellationToken)
    {
        var application = await archive.Find(postingId, applicationId, cancellationToken);
        if (application is null)
            return false;
        if (application.Status != ApplicationStatus.Ready || application.EducationAssessment is null
            || application.ReviewVersion != request.Version)
            throw new EducationReviewConflictException();
        application.ReviewEducation(request.OverrideMet, request.Note
            ?? throw new ArgumentNullException(nameof(request.Note)));
        await changes.Record(postingId, applicationId, cancellationToken);
        if (!await archive.Save(application, cancellationToken))
            throw new EducationReviewConflictException();
        return true;
    }
}
