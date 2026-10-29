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

    public Task<Posting?> FindById(int postingId, CancellationToken cancellationToken) =>
        context.Postings
            .AsNoTracking()
            .Where(entity => entity.Id == postingId)
            .Select(entity => new Posting(entity.Id, entity.Title, entity.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Posting?> Publish(int postingId, CancellationToken cancellationToken)
    {
        var entity = await context.Postings.SingleOrDefaultAsync(
            entity => entity.Id == postingId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = PostingStatus.Published;
        await context.SaveChangesAsync(cancellationToken);

        return new Posting(entity.Id, entity.Title, entity.Status);
    }

    public async Task<PostingList> List(int pageNumber, int postingsPerPage, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(postingsPerPage);
        var offset = ((long)pageNumber - 1) * postingsPerPage;

        // Both queries must observe the same snapshot if postings change concurrently.
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        var totalPostings = await context.Postings.LongCountAsync(cancellationToken);
        var postings = offset >= totalPostings
            ? []
            : await context.Postings
                .AsNoTracking()
                .OrderBy(entity => entity.Id)
                .Skip(checked((int)offset))
                .Take(postingsPerPage)
                .Select(entity => new Posting(entity.Id, entity.Title, entity.Status))
                .ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PostingList(postings, totalPostings);
    }
}
