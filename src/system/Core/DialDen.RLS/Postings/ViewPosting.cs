using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class ViewPosting : IViewPosting
{
    private readonly IPostingsArchive _postingsArchive;

    public ViewPosting(IPostingsArchive postingsArchive)
    {
        _postingsArchive = postingsArchive;
    }

    public async Task<ViewPostingResponse?> View(
        ViewPostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var posting = await _postingsArchive.FindById(request.Id, cancellationToken);
        return posting is null
            ? null
            : new ViewPostingResponse(request.Id, posting.Title);
    }
}
