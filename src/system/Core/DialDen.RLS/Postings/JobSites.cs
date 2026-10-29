using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class JobSites : IJobSites
{
    public IReadOnlyList<JobSite> List() => [new JobSite("jobillico", "Jobillico")];
}
