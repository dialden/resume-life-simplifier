using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

internal sealed class ExtractApplicationInformation(
    IApplicationsArchive archive, IResumeContactExtraction extraction) : IExtractApplicationInformation
{
    public async Task<bool> Extract(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        var application = await archive.Find(postingId, applicationId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Application {applicationId} for posting {postingId} was not found for information extraction.");
        if (application.ExtractionStatus != InformationExtractionStatus.Pending)
            return false;

        var contacts = await extraction.Extract(
            application.ResumeFileName, application.Resume, cancellationToken);
        application.CompleteExtraction(contacts);
        return await archive.Save(application, cancellationToken);
    }
}
