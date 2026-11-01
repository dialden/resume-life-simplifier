using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class JobSites(IJobSitesCatalog catalog) : IJobSites
{
    public Task<IReadOnlyList<JobSite>> List(CancellationToken cancellationToken) =>
        catalog.ListAvailable(cancellationToken);
}
