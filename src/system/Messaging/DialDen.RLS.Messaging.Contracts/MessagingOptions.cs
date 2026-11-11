namespace DialDen.RLS.Messaging.Contracts;

/// <summary>Shared by the processes that send and consume messages.</summary>
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string JobSitePublicationQueue { get; set; } = "rls-job-site-publication";

    public string ApplicationReceivedTopic { get; set; } = "rls-application-received";

    public string ResumeExtractionQueue { get; set; } = "rls-resume-extraction";
}
