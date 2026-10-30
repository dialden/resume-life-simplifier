namespace DialDen.RLS.Messaging.Contracts;

/// <summary>Shared by the processes that send and consume the commands.</summary>
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string JobSitePublicationQueue { get; set; } = "rls-job-site-publication";
}
