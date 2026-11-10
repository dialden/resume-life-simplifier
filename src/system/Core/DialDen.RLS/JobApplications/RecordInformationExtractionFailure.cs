using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

internal sealed class RecordInformationExtractionFailure(IApplicationsArchive archive)
    : IRecordInformationExtractionFailure
{
    public async Task<bool> Record(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        var application = await archive.Find(postingId, applicationId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Application {applicationId} for posting {postingId} was not found for information extraction failure.");
        if (application.ExtractionStatus != InformationExtractionStatus.Pending)
            return false;

        application.FailExtraction();
        return await archive.Save(application, cancellationToken);
    }
}
