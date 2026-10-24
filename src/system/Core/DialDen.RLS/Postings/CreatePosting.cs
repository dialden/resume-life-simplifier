using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class CreatePosting : ICreatePosting
{
    private readonly IPostingsArchive _postingsArchive;

    public CreatePosting(IPostingsArchive postingsArchive)
    {
        _postingsArchive = postingsArchive;
    }

    public Task<CreatePostingResponse> Create(
        CreatePostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var posting = new Posting(request.Title);
        return CreateResponse(posting, cancellationToken);
    }

    private async Task<CreatePostingResponse> CreateResponse(
        Posting posting,
        CancellationToken cancellationToken)
    {
        var id = await _postingsArchive.Add(posting, cancellationToken);
        return new CreatePostingResponse(id, posting.Title);
    }
}
