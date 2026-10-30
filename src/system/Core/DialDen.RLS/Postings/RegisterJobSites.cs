using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.JobSites;

namespace DialDen.RLS.Postings;

internal sealed class RegisterJobSites(IEnumerable<IJobSite> jobSites, IJobSitesCatalog catalog) : IRegisterJobSites
{
    public Task Register(CancellationToken cancellationToken)
    {
        var available = jobSites.Select(jobSite => new JobSite(jobSite.Id, jobSite.Name)).ToArray();
        var duplicate = available.GroupBy(jobSite => jobSite.Id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Several job sites are identified by '{duplicate.Key}'.");
        }

        return catalog.MakeAvailableOnly(available, cancellationToken);
    }
}
