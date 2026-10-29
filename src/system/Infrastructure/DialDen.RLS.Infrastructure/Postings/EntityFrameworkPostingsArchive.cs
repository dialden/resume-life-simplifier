using System.Data;
using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Postings;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class EntityFrameworkPostingsArchive(RLSContext context) : IPostingsArchive
{
    public async Task<Posting> Add(string title, CancellationToken cancellationToken)
    {
        var entity = new PostingEntity { Title = title };
        context.Postings.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new Posting(entity.Id, entity.Title, entity.Status);
    }

    public async Task<Posting?> FindById(int postingId, CancellationToken cancellationToken)
    {
        var entity = await context.Postings
            .AsNoTracking()
            .Include(entity => entity.Deliveries)
            .SingleOrDefaultAsync(entity => entity.Id == postingId, cancellationToken);
        return entity is null ? null : ToPosting(entity);
    }

    public async Task<Posting?> Publish(
        int postingId, IReadOnlyList<string> jobSiteIds, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await context.Postings.SingleOrDefaultAsync(
            entity => entity.Id == postingId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var published = ToPosting(entity).Publish(jobSiteIds);
        entity.Status = published.Status;
        try
        {
            // Claim publication before inserting deliveries, so concurrent requests cannot insert duplicates.
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new PostingAlreadyPublishedException();
        }
        entity.Deliveries = published.Deliveries.Select(delivery => new PostingDeliveryEntity
        {
            PostingId = postingId,
            JobSiteId = delivery.JobSiteId,
            Status = delivery.Status
        }).ToList();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return published;
    }

    private static Posting ToPosting(PostingEntity entity) => new(entity.Id, entity.Title, entity.Status)
    {
        Deliveries = entity.Deliveries
            .OrderBy(delivery => delivery.JobSiteId, StringComparer.Ordinal)
            .Select(delivery => new PostingDelivery(delivery.JobSiteId, delivery.Status))
            .ToArray()
    };

    public async Task<PostingList> List(int pageNumber, int postingsPerPage, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(postingsPerPage);
        var offset = ((long)pageNumber - 1) * postingsPerPage;

        // Both queries must observe the same snapshot if postings change concurrently.
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        var totalPostings = await context.Postings.LongCountAsync(cancellationToken);
        var entities = offset >= totalPostings
            ? []
            : await context.Postings
                .AsNoTracking()
                .Include(entity => entity.Deliveries)
                .OrderBy(entity => entity.Id)
                .Skip(checked((int)offset))
                .Take(postingsPerPage)
                .ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PostingList(entities.Select(ToPosting).ToArray(), totalPostings);
    }
}
