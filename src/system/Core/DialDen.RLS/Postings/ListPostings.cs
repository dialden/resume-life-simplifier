using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class ListPostings : IListPostings
{
    private readonly IPostingsArchive _postingsArchive;

    public ListPostings(IPostingsArchive postingsArchive)
    {
        _postingsArchive = postingsArchive;
    }

    public async Task<IReadOnlyList<ViewPostingResponse>> List(CancellationToken cancellationToken)
    {
        var postings = await _postingsArchive.List(cancellationToken);
        return postings.Select(posting => new ViewPostingResponse(posting.Id, posting.Posting.Title)).ToArray();
    }
}
