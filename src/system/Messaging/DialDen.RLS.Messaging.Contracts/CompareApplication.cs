namespace DialDen.RLS.Messaging.Contracts;

public sealed record CompareApplication(int PostingId, int ApplicationId)
{
    public const string QueueName = "rls-application-comparison";
}
