using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public interface IJobSitesCatalog
{
    Task<IReadOnlyList<JobSite>> ListAvailable(CancellationToken cancellationToken);

    /// <summary>Makes exactly these job sites available. The other known job sites become unavailable.</summary>
    Task MakeAvailableOnly(IReadOnlyList<JobSite> jobSites, CancellationToken cancellationToken);
}
