namespace DialDen.RLS.Abstractions.Postings;

public sealed record PublishPostingResponse(int Id, PostingStatus Status)
{
    public IReadOnlyList<PostingDeliveryResponse> Deliveries { get; init; } = [];
}
