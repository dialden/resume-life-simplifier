using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Postings;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Publishing;

internal sealed class EntityFrameworkJobSitePublications(RLSContext context) : IJobSitePublications
{
    public async Task Add(JobSitePublication publication, CancellationToken cancellationToken)
    {
        await context.JobSitePublications.AddAsync(publication, cancellationToken);
    }

    public async Task<JobSitePublication?> Find(int postingId, string jobSiteId, CancellationToken cancellationToken)
    {
        return await context.JobSitePublications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.PostingId == postingId && entity.JobSiteId == jobSiteId, cancellationToken);
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
                    .ToArray());
    }
}
