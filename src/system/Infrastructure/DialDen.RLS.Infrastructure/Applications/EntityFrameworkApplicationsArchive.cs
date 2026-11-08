using DialDen.RLS.JobApplications;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class EntityFrameworkApplicationsArchive(RLSContext context) : IApplicationsArchive
{
    public async Task<int> Add(Application application, CancellationToken cancellationToken)
    {
        context.Applications.Add(application);
        await context.SaveChangesAsync(cancellationToken);
        return application.Id;
    }

    public async Task<IReadOnlyList<ApplicationSummary>> List(int postingId, CancellationToken cancellationToken)
    {
        return await context.Applications.AsNoTracking().Where(item => item.PostingId == postingId)
            .OrderBy(item => item.Id)
            .Select(item => new ApplicationSummary(item.Id, item.Name, item.Email, item.Source, item.ReferralJobSiteId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Application?> Find(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        return await context.Applications.AsNoTracking()
            .SingleOrDefaultAsync(item => item.PostingId == postingId && item.Id == applicationId, cancellationToken);
    }
}
