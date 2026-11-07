using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class ViewPosting : IViewPosting
{
    private readonly IPostingsArchive _postingsArchive;
    private readonly IJobSitePublications _jobSitePublications;
    private readonly IPublicListingAddress _publicListingAddress;

    public ViewPosting(
        IPostingsArchive postingsArchive,
        IJobSitePublications jobSitePublications,
        IPublicListingAddress publicListingAddress)
    {
        _postingsArchive = postingsArchive;
        _jobSitePublications = jobSitePublications;
        _publicListingAddress = publicListingAddress;
    }

    public async Task<ViewPostingResponse?> View(
        ViewPostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var posting = await _postingsArchive.FindById(request.Id, cancellationToken);
        if (posting is null)
        {
            return null;
        }

        var publications = await _jobSitePublications.ListFor(posting.Id, cancellationToken);
        return new ViewPostingResponse(posting.Id, posting.Title, posting.Status)
        {
            Description = posting.Description,
            PublicListingUrl = posting.Status == PostingStatus.Published
                ? _publicListingAddress.ForPosting(posting.Id)
                : null,
            JobSitePublications = publications.Select(ToResponse).ToArray()
        };
    }

    private static JobSitePublicationResponse ToResponse(JobSitePublication publication) =>
        new(publication.JobSiteId, publication.Status);
}
