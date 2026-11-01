namespace DialDen.RLS.Worker.Root;

public sealed class JobSitesOptions
{
    public const string SectionName = "JobSites";

    public string Type { get; set; } = "real";

    public List<StubJobSiteOptions> Sites { get; set; } = [];
}
