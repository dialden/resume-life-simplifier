namespace DialDen.RLS.Infrastructure.JobSites;

internal sealed class JobSiteEntity
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public bool IsAvailable { get; set; }
}
