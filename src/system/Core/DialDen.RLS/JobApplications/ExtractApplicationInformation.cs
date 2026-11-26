using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Postings;

namespace DialDen.RLS.JobApplications;

internal sealed class ExtractApplicationInformation(
    IApplicationsArchive archive, IPostingsArchive postings, IResumeInformationExtraction extraction) : IExtractApplicationInformation
{
    public async Task<bool> Extract(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        var application = await archive.Find(postingId, applicationId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Application {applicationId} for posting {postingId} was not found for information extraction.");
        if (application.Status != ApplicationStatus.AwaitingExtraction)
            return false;

        var posting = await postings.FindById(postingId, cancellationToken)
            ?? throw new InvalidOperationException($"Posting {postingId} was not found.");
        var information = await extraction.Extract(
            application.ResumeFileName, application.Resume, cancellationToken);
        application.CompleteExtraction(information, posting.RequiredEducation is not null);
        return await archive.Save(application, cancellationToken);
    }
}
