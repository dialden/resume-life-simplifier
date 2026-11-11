using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public interface IResumeContactExtraction
{
    Task<ExtractedContactInformation> Extract(
        string resumeFileName, byte[] resume, CancellationToken cancellationToken);
}
