using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class PublishPosting : IPublishPosting
{
    private readonly IPostingsArchive _postingsArchive;
    private readonly IJobSites _jobSites;

    public PublishPosting(IPostingsArchive postingsArchive, IJobSites jobSites)
    {
        _postingsArchive = postingsArchive;
        _jobSites = jobSites;
    }

    public async Task<PublishPostingResponse?> Publish(
        PublishPostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var availableIds = _jobSites.List().Select(site => site.Id).ToHashSet(StringComparer.Ordinal);
        if (request.JobSiteIds is null ||
            request.JobSiteIds.Distinct(StringComparer.Ordinal).Count() != request.JobSiteIds.Count ||
            request.JobSiteIds.Any(id => !availableIds.Contains(id)))
        {
            throw new InvalidJobSitesException();
        }

        var posting = await _postingsArchive.Publish(request.Id, request.JobSiteIds, cancellationToken);
        return posting is null ? null : new PublishPostingResponse(posting.Id, posting.Status)
        {
            Deliveries = posting.Deliveries
                .Select(delivery => new PostingDeliveryResponse(delivery.JobSiteId, delivery.Status))
                .ToArray()
        };
    }
}
