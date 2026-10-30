namespace DialDen.RLS.JobSites.Jobillico;

internal sealed class JobillicoJobSite : IJobSite
{
    public string Id => "jobillico";

    public string Name => "Jobillico";

    // Not part of the demo: publishing on Jobillico always fails.
    public Task Publish(JobSitePosting posting, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Publishing on Jobillico is not implemented yet.");
}
