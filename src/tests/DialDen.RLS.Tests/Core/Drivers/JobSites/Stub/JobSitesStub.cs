using System.Collections.Concurrent;

namespace DialDen.RLS.Tests.Core.Drivers.JobSites.Stub;

/// <summary>
/// Shared by the tests and the stubbed job-site plugins of the in-process Worker.
/// Postings not concerned by any rule are accepted.
/// </summary>
public class JobSitesStub
{
    private readonly ConcurrentQueue<JobSitePostingRule> _rules = new();

    public void AcceptPostings(string jobSiteId, Func<int, bool> isConcernedPosting) =>
        _rules.Enqueue(new JobSitePostingRule(jobSiteId, isConcernedPosting, Accepts: true));

    public void RejectPostings(string jobSiteId, Func<int, bool> isConcernedPosting) =>
        _rules.Enqueue(new JobSitePostingRule(jobSiteId, isConcernedPosting, Accepts: false));

    public Task Publish(string jobSiteId, int postingId)
    {
        var rule = _rules.LastOrDefault(rule => rule.JobSiteId == jobSiteId && rule.IsConcernedPosting(postingId));
        if (rule is { Accepts: false })
        {
            throw new InvalidOperationException($"Job site '{jobSiteId}' rejected posting {postingId}.");
        }
        return Task.CompletedTask;
    }
}
