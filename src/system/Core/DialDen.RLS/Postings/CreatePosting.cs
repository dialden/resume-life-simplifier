using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class CreatePosting : ICreatePosting
{
    private readonly IPostingsArchive _postingsArchive;

    public CreatePosting(IPostingsArchive postingsArchive)
    {
        _postingsArchive = postingsArchive;
    }

    public async Task<CreatePostingResponse> Create(
        CreatePostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var posting = await _postingsArchive.Add(request.Title, cancellationToken);
        return new CreatePostingResponse(posting.Id, posting.Title);
    }
}
