using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class ListPostings : IListPostings
{
    private readonly IPostingsArchive _postingsArchive;
    private readonly IJobSitePublications _jobSitePublications;

    public ListPostings(IPostingsArchive postingsArchive, IJobSitePublications jobSitePublications)
    {
        _postingsArchive = postingsArchive;
        _jobSitePublications = jobSitePublications;
    }

    public async Task<ListPostingsResponse> List(ListPostingsRequest request, CancellationToken cancellationToken)
    {
        var list = await _postingsArchive.List(request.PageNumber, ListPostingsRequest.PageSize, cancellationToken);
        var publications = await _jobSitePublications.ListFor(
            list.Postings.Select(posting => posting.Id).ToArray(), cancellationToken);
        return new ListPostingsResponse(
            list.Postings.Select(posting => new ViewPostingResponse(posting.Id, posting.Title, posting.Status)
            {
                JobSitePublications = publications.GetValueOrDefault(posting.Id, [])
                    .Select(publication => new JobSitePublicationResponse(publication.JobSiteId, publication.Status))
                    .ToArray()
            }).ToArray(),
            list.TotalPostings, request.PageNumber, ListPostingsRequest.PageSize);
    }
}
