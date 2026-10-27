using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class ListPostings : IListPostings
{
    private readonly IPostingsArchive _postingsArchive;

    public ListPostings(IPostingsArchive postingsArchive)
    {
        _postingsArchive = postingsArchive;
    }

    public async Task<ListPostingsResponse> List(ListPostingsRequest request, CancellationToken cancellationToken)
    {
        var list = await _postingsArchive.List(request.PageNumber, ListPostingsRequest.PageSize, cancellationToken);
        return new ListPostingsResponse(
            list.Postings.Select(posting => new ViewPostingResponse(posting.Id, posting.Title, posting.Status)).ToArray(),
            list.TotalPostings, request.PageNumber, ListPostingsRequest.PageSize);
    }
}
