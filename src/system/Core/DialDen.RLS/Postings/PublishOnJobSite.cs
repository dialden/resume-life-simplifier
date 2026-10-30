using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.JobSites;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.Postings;

internal sealed class PublishOnJobSite(
    IJobSitePublications jobSitePublications,
    IPostingsArchive postingsArchive,
    IEnumerable<IJobSite> jobSites,
    ILogger<PublishOnJobSite> logger) : IPublishOnJobSite
{
    public async Task Publish(PublishOnJobSiteRequest request, CancellationToken cancellationToken)
    {
        var publication = await jobSitePublications.Find(request.PostingId, request.JobSiteId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Posting {request.PostingId} was not requested on job site '{request.JobSiteId}'.");
        if (!publication.IsPending)
        {
            return;
        }

        var posting = await postingsArchive.FindById(request.PostingId, cancellationToken)
            ?? throw new InvalidOperationException($"Posting {request.PostingId} does not exist.");
        var jobSite = jobSites.SingleOrDefault(site => site.Id == request.JobSiteId);
        await jobSitePublications.Save(await PublishOn(jobSite, posting, publication, cancellationToken), cancellationToken);
    }

    private async Task<JobSitePublication> PublishOn(
        IJobSite? jobSite, Posting posting, JobSitePublication publication, CancellationToken cancellationToken)
    {
        if (jobSite is null)
        {
            logger.LogWarning("Job site {JobSiteId} is not available to publish posting {PostingId}.",
                publication.JobSiteId, posting.Id);
            return publication.Fail();
        }

        try
        {
            await jobSite.Publish(new JobSitePosting(posting.Id, posting.Title), cancellationToken);
            return publication.Succeed();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Job site {JobSiteId} did not accept posting {PostingId}.",
                publication.JobSiteId, posting.Id);
            return publication.Fail();
        }
    }
}
