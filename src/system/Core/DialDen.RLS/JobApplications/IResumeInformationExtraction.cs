namespace DialDen.RLS.JobApplications;

public interface IResumeInformationExtraction
{
    Task<ExtractedResumeInformation> Extract(
        string resumeFileName, byte[] resume, CancellationToken cancellationToken);
}
