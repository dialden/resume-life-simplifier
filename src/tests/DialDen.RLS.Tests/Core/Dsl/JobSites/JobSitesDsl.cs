using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.JobSites;

namespace DialDen.RLS.Tests.Core.Dsl.JobSites;

public class JobSitesDsl
{
    private readonly IJobSitesDriver _driver;
    private readonly UseCaseContext _context;

    public JobSitesDsl(IJobSitesDriver driver, UseCaseContext context)
    {
        _driver = driver;
        _context = context;
    }

    /// <summary>Only the postings created by the current scenario are concerned.</summary>
    public void AcceptPostings(string jobSiteId) => _driver.AcceptPostings(jobSiteId, IsScenarioPosting);

    /// <summary>Only the postings created by the current scenario are concerned.</summary>
    public void RejectPostings(string jobSiteId) => _driver.RejectPostings(jobSiteId, IsScenarioPosting);

    private bool IsScenarioPosting(int postingId) =>
        _context.HasResultValue(postingId.ToString(CultureInfo.InvariantCulture));
}
