using DialDen.RLS.Tests.Core.Drivers.JobSites;

namespace DialDen.RLS.Tests.Core.Dsl.External.JobSites;

public class JobSitesDsl
{
    // Long enough for the customer to open the posting while its publication is still pending.
    private static readonly TimeSpan SlowResponseDelay = TimeSpan.FromSeconds(5);

    private readonly IJobSitesDriver _driver;
    private readonly UseCaseContext _context;
    private readonly Dictionary<string, bool> _responses = new(StringComparer.Ordinal);
    private readonly HashSet<string> _slowJobSites = new(StringComparer.Ordinal);

    public JobSitesDsl(IJobSitesDriver driver, UseCaseContext context)
    {
        _driver = driver;
        _context = context;
    }

    public Task Reset() => _driver.ResetAndAcceptByDefault();

    public void AcceptPostings(string jobSiteId) => _responses[jobSiteId] = true;

    public void RejectPostings(string jobSiteId) => _responses[jobSiteId] = false;

    public void RespondSlowly(string jobSiteId) => _slowJobSites.Add(jobSiteId);

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
            var slow = _slowJobSites.Contains(jobSiteId);
            var configured = _responses.TryGetValue(jobSiteId, out var accepts);
            if (configured || slow)
            {
                await _driver.SetResponse(
                    jobSiteId, postingId, accepts || !configured, slow ? SlowResponseDelay : TimeSpan.Zero);
            }
        }
    }
}
