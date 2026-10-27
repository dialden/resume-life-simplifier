using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class PublishPosting : IPublishPosting
{
    private readonly IPostingsArchive _postingsArchive;

    public PublishPosting(IPostingsArchive postingsArchive)
    {
        _postingsArchive = postingsArchive;
    }

    public async Task<PublishPostingResponse?> Publish(
        PublishPostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var posting = await _postingsArchive.Publish(request.Id, cancellationToken);
        return posting is null ? null : new PublishPostingResponse(posting.Id, posting.Status);
    }
}
