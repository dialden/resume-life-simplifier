using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class PostingDeliveryEntity
{
    public int PostingId { get; set; }

    public required string JobSiteId { get; set; }

    public PostingDeliveryStatus Status { get; set; } = PostingDeliveryStatus.Pending;
}
