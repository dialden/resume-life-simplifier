using System.Data;
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

        return ToPosting(entity);
    }

    public async Task<Posting?> FindById(int postingId, CancellationToken cancellationToken)
    {
        // Tracked, so Save can detect changes made after the posting was found.
        var entity = await context.Postings.SingleOrDefaultAsync(
            entity => entity.Id == postingId, cancellationToken);
        return entity is null ? null : ToPosting(entity);
    }

    public async Task Save(Posting posting, CancellationToken cancellationToken)
    {
        var entity = context.Postings.Local.SingleOrDefault(entity => entity.Id == posting.Id)
            ?? throw new InvalidOperationException("Find the posting before saving it.");
        var found = context.Entry(entity).OriginalValues.ToObject() as PostingEntity
            ?? throw new InvalidOperationException("The found posting could not be read.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Claim the change before saving pending work such as job-site publication requests, so
        // concurrent saves fail here instead of inserting duplicates.
        var claimed = await context.Postings
            .Where(stored => stored.Id == found.Id && stored.Title == found.Title && stored.Status == found.Status)
            .ExecuteUpdateAsync(update => update
                .SetProperty(stored => stored.Title, posting.Title)
                .SetProperty(stored => stored.Status, posting.Status), cancellationToken);
        if (claimed == 0)
        {
            throw new PostingChangedConcurrentlyException();
        }

        entity.Title = posting.Title;
        entity.Status = posting.Status;
        context.Entry(entity).State = EntityState.Unchanged;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
        var entities = offset >= totalPostings
            ? []
            : await context.Postings
                .AsNoTracking()
                .OrderBy(entity => entity.Id)
                .Skip(checked((int)offset))
                .Take(postingsPerPage)
                .ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PostingList(entities.Select(ToPosting).ToArray(), totalPostings);
    }

    private static Posting ToPosting(PostingEntity entity) => new(entity.Id, entity.Title, entity.Status);
}
