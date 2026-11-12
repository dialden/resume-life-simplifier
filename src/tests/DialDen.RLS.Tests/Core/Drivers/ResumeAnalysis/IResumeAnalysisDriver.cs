using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;

public interface IResumeAnalysisDriver
{
    Task ResetAndReturnNoContactsByDefault();

    Task FindsContactInformation(ExtractedContactInformation contacts, bool holdResponse);

    Task FailsAnalysis();
}
