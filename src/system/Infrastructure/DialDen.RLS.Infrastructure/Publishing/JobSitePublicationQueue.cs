namespace DialDen.RLS.Infrastructure.Publishing;

internal static class JobSitePublicationQueue
{
    public static readonly Uri Address = new("queue:rls-job-site-publication");
}
