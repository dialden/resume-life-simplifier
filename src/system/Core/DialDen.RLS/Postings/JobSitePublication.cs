using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public sealed record JobSitePublication(int PostingId, string JobSiteId, JobSitePublicationStatus Status)
{
    public bool IsPending => Status == JobSitePublicationStatus.Pending;

    public JobSitePublication Succeed() => RespondedWith(JobSitePublicationStatus.Succeeded);

    public JobSitePublication Fail() => RespondedWith(JobSitePublicationStatus.Failed);

    private JobSitePublication RespondedWith(JobSitePublicationStatus status)
    {
        if (!IsPending)
        {
            throw new InvalidOperationException(
                $"Job site '{JobSiteId}' has already responded for posting {PostingId}.");
        }

        return this with { Status = status };
    }
}
