using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Messaging.Contracts;
using DialDen.RLS.Postings;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DialDen.RLS.Infrastructure.Publishing;

internal sealed class EntityFrameworkJobSitePublications(
    RLSContext context,
    ISendEndpointProvider sendEndpointProvider,
    IOptions<MessagingOptions> messagingOptions) : IJobSitePublications
{
    public async Task Request(Posting posting, IReadOnlyList<JobSite> jobSites, CancellationToken cancellationToken)
    {
        if (jobSites.Count == 0)
        {
            return;
        }

        // The bus outbox stores these commands in the context, so they commit with the next save.
        var endpoint = await sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{messagingOptions.Value.JobSitePublicationQueue}"));
        foreach (var jobSite in jobSites)
        {
            context.JobSitePublications.Add(new JobSitePublicationEntity
            {
                PostingId = posting.Id,
                JobSiteId = jobSite.Id
            });
            await endpoint.Send(new PublishPostingOnJobSite(posting.Id, jobSite.Id), cancellationToken);
        }
    }

    public async Task<JobSitePublication?> Find(int postingId, string jobSiteId, CancellationToken cancellationToken)
    {
        var entity = await context.JobSitePublications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.PostingId == postingId && entity.JobSiteId == jobSiteId, cancellationToken);
        return entity is null ? null : ToPublication(entity);
    }

    public async Task Save(JobSitePublication publication, CancellationToken cancellationToken)
    {
        var saved = await context.JobSitePublications
            .Where(stored => stored.PostingId == publication.PostingId
                && stored.JobSiteId == publication.JobSiteId
                && stored.Status == JobSitePublicationStatus.Pending)
            .ExecuteUpdateAsync(update => update.SetProperty(stored => stored.Status, publication.Status), cancellationToken);
        if (saved == 0)
        {
            throw new JobSitePublicationChangedConcurrentlyException(publication.PostingId, publication.JobSiteId);
        }
    }

    public async Task<IReadOnlyList<JobSitePublication>> ListFor(int postingId, CancellationToken cancellationToken)
    {
        var publications = await ListFor([postingId], cancellationToken);
        return publications.GetValueOrDefault(postingId, []);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<JobSitePublication>>> ListFor(
        IReadOnlyList<int> postingIds, CancellationToken cancellationToken)
    {
        if (postingIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<JobSitePublication>>();
        }

        var entities = await context.JobSitePublications
            .AsNoTracking()
            .Where(entity => postingIds.Contains(entity.PostingId))
            .ToListAsync(cancellationToken);
        return entities
            .GroupBy(entity => entity.PostingId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<JobSitePublication>)group
                    .OrderBy(entity => entity.JobSiteId, StringComparer.Ordinal)
                    .Select(ToPublication)
                    .ToArray());
    }

    private static JobSitePublication ToPublication(JobSitePublicationEntity entity) =>
        new(entity.PostingId, entity.JobSiteId, entity.Status);
}
