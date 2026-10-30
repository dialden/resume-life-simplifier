using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class JobSites : IJobSites
{
    private static readonly IReadOnlyList<JobSite> Available = [new JobSite("jobillico", "Jobillico")];

    public IReadOnlyList<JobSite> List() => Available;

    public IReadOnlyList<JobSite> Find(IReadOnlyList<string>? jobSiteIds)
    {
        if (jobSiteIds is null || jobSiteIds.Distinct(StringComparer.Ordinal).Count() != jobSiteIds.Count)
        {
            throw new InvalidJobSitesException();
        }

        return jobSiteIds
            .Select(id => Available.SingleOrDefault(site => site.Id == id) ?? throw new InvalidJobSitesException())
            .ToArray();
    }
}
