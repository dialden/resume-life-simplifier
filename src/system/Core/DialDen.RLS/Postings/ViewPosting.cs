using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class ViewPosting : IViewPosting
{
    private readonly IPostingsArchive _postingsArchive;
    private readonly IJobSitePublications _jobSitePublications;

    public ViewPosting(IPostingsArchive postingsArchive, IJobSitePublications jobSitePublications)
    {
        _postingsArchive = postingsArchive;
        _jobSitePublications = jobSitePublications;
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
            JobSitePublications = publications.Select(ToResponse).ToArray()
        };
    }

    private static JobSitePublicationResponse ToResponse(JobSitePublication publication) =>
        new(publication.JobSiteId, publication.Status);
}
