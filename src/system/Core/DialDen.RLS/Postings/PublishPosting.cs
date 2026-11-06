using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class PublishPosting : IPublishPosting
{
    private readonly IPostingsArchive _postingsArchive;
    private readonly IJobSitesCatalog _jobSitesCatalog;
    private readonly IJobSitePublications _jobSitePublications;
    private readonly IPublicListingAddress _publicListingAddress;

    public PublishPosting(
        IPostingsArchive postingsArchive,
        IJobSitesCatalog jobSitesCatalog,
        IJobSitePublications jobSitePublications,
        IPublicListingAddress publicListingAddress)
    {
        _postingsArchive = postingsArchive;
        _jobSitesCatalog = jobSitesCatalog;
        _jobSitePublications = jobSitePublications;
        _publicListingAddress = publicListingAddress;
    }

    public async Task<PublishPostingResponse?> Publish(
        PublishPostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jobSites = await Select(request.JobSiteIds, cancellationToken);
        var posting = await _postingsArchive.FindById(request.Id, cancellationToken);
        if (posting is null)
        {
            return null;
        }

        var published = posting.Publish();
        var publicListingUrl = _publicListingAddress.ForPosting(published.Id);
        await _jobSitePublications.Request(published, jobSites, publicListingUrl, cancellationToken);
        try
        {
            await _postingsArchive.Save(published, cancellationToken);
        }
        catch (PostingChangedConcurrentlyException)
        {
            // Publishing is the only change to a posting's status, so someone else published it first.
            throw new PostingAlreadyPublishedException();
        }

        return new PublishPostingResponse(published.Id, published.Status, publicListingUrl)
        {
            JobSitePublications = jobSites
                .Select(site => new JobSitePublicationResponse(site.Id, JobSitePublicationStatus.Pending))
                .OrderBy(publication => publication.JobSiteId, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private async Task<IReadOnlyList<JobSite>> Select(IReadOnlyList<string>? jobSiteIds, CancellationToken cancellationToken)
    {
        if (jobSiteIds is null || jobSiteIds.Distinct(StringComparer.Ordinal).Count() != jobSiteIds.Count)
        {
            throw new InvalidJobSitesException();
        }
        if (jobSiteIds.Count == 0)
        {
            return [];
        }

        var available = await _jobSitesCatalog.ListAvailable(cancellationToken);
        return jobSiteIds
            .Select(id => available.SingleOrDefault(site => site.Id == id) ?? throw new InvalidJobSitesException())
            .ToArray();
    }
}
