using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Postings;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Publishing;

internal sealed class EntityFrameworkJobSitePublications(
    RLSContext context,
    ISendEndpointProvider sendEndpointProvider) : IJobSitePublications
{
    public async Task Request(Posting posting, IReadOnlyList<JobSite> jobSites, CancellationToken cancellationToken)
    {
        if (jobSites.Count == 0)
        {
            return;
        }

        // The bus outbox stores these commands in the context, so they commit with the next save.
        var endpoint = await sendEndpointProvider.GetSendEndpoint(JobSitePublicationQueue.Address);
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
                    .Select(entity => new JobSitePublication(entity.PostingId, entity.JobSiteId, entity.Status))
                    .ToArray());
    }
}
