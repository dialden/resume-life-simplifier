using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Postings;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.JobSites;

internal sealed class EntityFrameworkJobSitesCatalog(RLSContext context) : IJobSitesCatalog
{
    public async Task<IReadOnlyList<JobSite>> ListAvailable(CancellationToken cancellationToken) =>
        await context.JobSites
            .AsNoTracking()
            .Where(entity => entity.IsAvailable)
            .OrderBy(entity => entity.Id)
            .Select(entity => new JobSite(entity.Id, entity.Name))
            .ToListAsync(cancellationToken);

    public async Task MakeAvailableOnly(IReadOnlyList<JobSite> jobSites, CancellationToken cancellationToken)
    {
        // Unavailable job sites are kept, since existing publications still refer to them.
        var known = await context.JobSites.ToDictionaryAsync(entity => entity.Id, cancellationToken);
        foreach (var entity in known.Values)
        {
            entity.IsAvailable = false;
        }
        foreach (var jobSite in jobSites)
        {
            if (!known.TryGetValue(jobSite.Id, out var entity))
            {
                entity = new JobSiteEntity { Id = jobSite.Id, Name = jobSite.Name };
                context.JobSites.Add(entity);
            }
            entity.Name = jobSite.Name;
            entity.IsAvailable = true;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
