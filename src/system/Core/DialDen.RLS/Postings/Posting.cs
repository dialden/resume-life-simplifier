using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public sealed record Posting(int Id, string Title, PostingStatus Status)
{
    public IReadOnlyList<PostingDelivery> Deliveries { get; init; } = [];

    public Posting Publish(IReadOnlyList<string> jobSiteIds)
    {
        if (Status == PostingStatus.Published)
        {
            throw new PostingAlreadyPublishedException();
        }

        return this with
        {
            Status = PostingStatus.Published,
            Deliveries = jobSiteIds
                .Select(id => new PostingDelivery(id, PostingDeliveryStatus.Pending))
                .ToArray()
        };
    }
}
