using DialDen.RLS.Tests.Core.Dsl.External.JobSites;

namespace DialDen.RLS.Tests.Core.Dsl;

/// <summary>Entry point for the DSLs of external systems. Add one DSL per external system here.</summary>
public class ExternalSystemsDsl
{
    public JobSitesDsl JobSites { get; }

    public ExternalSystemsDsl(JobSitesDsl jobSites)
    {
        JobSites = jobSites;
    }
}
