using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Postings;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.JobSites;

internal sealed class EntityFrameworkJobSitesCatalog(RLSContext context) : IJobSitesCatalog
{
    public async Task<IReadOnlyList<JobSite>> ListAvailable(CancellationToken cancellationToken) =>
        await context.JobSites
            .AsNoTracking()
            .Where(entity => EF.Property<bool>(entity, "IsAvailable"))
            .OrderBy(entity => entity.Id)
            .ToListAsync(cancellationToken);

    public async Task MakeAvailableOnly(IReadOnlyList<JobSite> jobSites, CancellationToken cancellationToken)
    {
        // Unavailable job sites are kept, since existing publications still refer to them.
        var known = await context.JobSites.ToDictionaryAsync(entity => entity.Id, cancellationToken);
        foreach (var entity in known.Values)
        {
            context.Entry(entity).Property<bool>("IsAvailable").CurrentValue = false;
        }
        foreach (var jobSite in jobSites)
        {
            if (!known.TryGetValue(jobSite.Id, out var entity))
            {
                entity = jobSite;
                context.JobSites.Add(entity);
                known.Add(jobSite.Id, entity);
            }
            context.Entry(entity).CurrentValues.SetValues(jobSite);
            context.Entry(entity).Property<bool>("IsAvailable").CurrentValue = true;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
