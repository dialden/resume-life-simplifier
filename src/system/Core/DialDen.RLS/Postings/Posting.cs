using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public sealed record Posting(int Id, string Title, PostingStatus Status)
{
    public Posting Publish()
    {
        if (Status == PostingStatus.Published)
        {
            throw new PostingAlreadyPublishedException();
        }

        return this with { Status = PostingStatus.Published };
    }
}
