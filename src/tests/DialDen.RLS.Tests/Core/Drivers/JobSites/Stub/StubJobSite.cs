using DialDen.RLS.JobSites;

namespace DialDen.RLS.Tests.Core.Drivers.JobSites.Stub;

/// <summary>Plugged into the system in place of a real job site, answering as the tests ask.</summary>
public sealed class StubJobSite(string id, string name, JobSitesStub stub) : IJobSite
{
    public string Id => id;

    public string Name => name;

    public Task Publish(JobSitePosting posting, CancellationToken cancellationToken) =>
        stub.Publish(id, posting.PostingId);
}
