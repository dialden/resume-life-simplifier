using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Postings;

namespace DialDen.RLS.JobApplications;

internal sealed class CompareApplication(
    IApplicationsArchive applications, IPostingsArchive postings, IEducationComparison comparison)
    : ICompareApplication
{
    public async Task<bool> Compare(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        var application = await applications.Find(postingId, applicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {applicationId} on posting {postingId} was not found.");
        if (application.Status != ApplicationStatus.AwaitingComparison)
            return false;
        var posting = await postings.FindById(postingId, cancellationToken)
            ?? throw new InvalidOperationException($"Posting {postingId} was not found.");
        var requirement = posting.RequiredEducation
            ?? throw new InvalidOperationException($"Posting {postingId} has no education requirement.");
        var education = application.ExtractedEducation
            ?? throw new InvalidOperationException($"Application {applicationId} has no extracted education.");
        var assessment = await comparison.Compare(requirement, education, cancellationToken);
        application.CompleteComparison(assessment.Met, assessment.Confidence, assessment.Evidence);
        return await applications.Save(application, cancellationToken);
    }

    public async Task<bool> RecordFailure(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        var application = await applications.Find(postingId, applicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {applicationId} on posting {postingId} was not found.");
        if (application.Status != ApplicationStatus.AwaitingComparison)
            return false;
        application.FailComparison();
        return await applications.Save(application, cancellationToken);
    }
}
