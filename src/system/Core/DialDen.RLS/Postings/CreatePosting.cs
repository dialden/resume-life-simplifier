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

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidPostingDescriptionException();
        }
        var draft = new Posting(0, request.Title, PostingStatus.Draft, request.RequiredEducation)
        {
            Description = request.Description
        };
        var posting = await _postingsArchive.Add(draft, cancellationToken);
        return new CreatePostingResponse(posting.Id, posting.Title, posting.Status)
        {
            Description = posting.Description,
            RequiredEducation = posting.RequiredEducation
        };
    }
}
