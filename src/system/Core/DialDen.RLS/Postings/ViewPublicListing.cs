using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class ViewPublicListing(
    IPostingsArchive postingsArchive,
    IPublicListingAddress publicListingAddress) : IViewPublicListing
{
    public async Task<PublicListingResponse?> View(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var posting = await postingsArchive.FindById(id, cancellationToken);
        if (posting is not { Status: PostingStatus.Published })
        {
            return null;
        }

        return new PublicListingResponse(
            posting.Id, posting.Title, posting.Description, publicListingAddress.ForPosting(posting.Id));
    }
}
