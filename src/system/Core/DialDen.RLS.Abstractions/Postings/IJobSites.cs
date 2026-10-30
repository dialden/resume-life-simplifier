namespace DialDen.RLS.Abstractions.Postings;

public interface IJobSites
{
    IReadOnlyList<JobSite> List();

    /// <exception cref="InvalidJobSitesException">The identifiers are missing, duplicated, or unavailable.</exception>
    IReadOnlyList<JobSite> Find(IReadOnlyList<string>? jobSiteIds);
}
