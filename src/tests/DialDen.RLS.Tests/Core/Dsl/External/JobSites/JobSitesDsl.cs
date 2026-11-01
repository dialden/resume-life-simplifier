using DialDen.RLS.Tests.Core.Drivers.JobSites;

namespace DialDen.RLS.Tests.Core.Dsl.External.JobSites;

public class JobSitesDsl
{
    private readonly IJobSitesDriver _driver;
    private readonly Dictionary<string, bool> _responses = new(StringComparer.Ordinal);

    public JobSitesDsl(IJobSitesDriver driver)
    {
        _driver = driver;
    }

    public Task Reset() => _driver.ResetAndAcceptByDefault();

    public void AcceptPostings(string jobSiteId) => _responses[jobSiteId] = true;

    public void RejectPostings(string jobSiteId) => _responses[jobSiteId] = false;

    public async Task ConfigureForPosting(int postingId, IReadOnlyList<string> jobSiteIds)
    {
        foreach (var jobSiteId in jobSiteIds)
        {
            if (_responses.TryGetValue(jobSiteId, out var accepts))
            {
                await _driver.SetResponse(jobSiteId, postingId, accepts);
            }
        }
    }
}
