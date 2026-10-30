using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class PostingEntity
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public PostingStatus Status { get; set; } = PostingStatus.Draft;
}
