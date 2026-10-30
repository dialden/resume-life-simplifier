using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

internal sealed class PublishPosting : IPublishPosting
{
    private readonly IPostingsArchive _postingsArchive;
    private readonly IJobSites _jobSites;
    private readonly IJobSitePublications _jobSitePublications;

    public PublishPosting(
        IPostingsArchive postingsArchive,
        IJobSites jobSites,
        IJobSitePublications jobSitePublications)
    {
        _postingsArchive = postingsArchive;
        _jobSites = jobSites;
        _jobSitePublications = jobSitePublications;
    }

    public async Task<PublishPostingResponse?> Publish(
        PublishPostingRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jobSites = _jobSites.Find(request.JobSiteIds);
        var posting = await _postingsArchive.FindById(request.Id, cancellationToken);
        if (posting is null)
        {
            return null;
        }

        var published = posting.Publish();
        await _jobSitePublications.Request(published, jobSites, cancellationToken);
        try
        {
            await _postingsArchive.Save(published, cancellationToken);
        }
        catch (PostingChangedConcurrentlyException)
        {
            // Publishing is the only change to a posting's status, so someone else published it first.
            throw new PostingAlreadyPublishedException();
        }

        return new PublishPostingResponse(published.Id, published.Status)
        {
            JobSitePublications = jobSites
                .Select(site => new JobSitePublicationResponse(site.Id, JobSitePublicationStatus.Pending))
                .OrderBy(publication => publication.JobSiteId, StringComparer.Ordinal)
                .ToArray()
        };
    }
}
