using DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.External.ResumeAnalysis;

public sealed class ResumeAnalysisDsl(IResumeAnalysisDriver driver)
{
    public Task FailsAnalysis() => driver.FailsAnalysis();

    public Task Reset() => driver.ResetAndReturnNoContactsByDefault();

    public Task FindsContactInformation(ExtractedContactInformation contacts, bool holdResponse) =>
        driver.FindsContactInformation(contacts, holdResponse);
}
