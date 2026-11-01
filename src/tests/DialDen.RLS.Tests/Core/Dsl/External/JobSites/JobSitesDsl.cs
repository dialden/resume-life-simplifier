using DialDen.RLS.Tests.Core.Drivers.JobSites;

namespace DialDen.RLS.Tests.Core.Dsl.External.JobSites;

public class JobSitesDsl
{
    private readonly IJobSitesDriver _driver;
    private readonly UseCaseContext _context;
    private readonly Dictionary<string, bool> _responses = new(StringComparer.Ordinal);

    public JobSitesDsl(IJobSitesDriver driver, UseCaseContext context)
    {
        _driver = driver;
        _context = context;
    }

    public Task Reset() => _driver.ResetAndAcceptByDefault();

    public void AcceptPostings(string jobSiteId) => _responses[jobSiteId] = true;

    public void RejectPostings(string jobSiteId) => _responses[jobSiteId] = false;

    public Task ConfigureForPosting(string postingAlias, IReadOnlyList<string> jobSiteIds)
    {
        if (!int.TryParse(_context.GetResultValue(postingAlias),
            System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var postingId))
        {
            throw new InvalidOperationException($"Posting alias '{postingAlias}' did not resolve to an integer ID.");
        }
        return ConfigureForPosting(postingId, jobSiteIds);
    }

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
