using DialDen.RLS.Applications;
using DialDen.RLS.Infrastructure.Postings;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class EntityFrameworkApplicationsArchive(RLSContext context) : IApplicationsArchive
{
    public async Task<int> Add(Application application, CancellationToken cancellationToken)
    {
        var entity = new ApplicationEntity
        {
            PostingId = application.PostingId, Name = application.Name, Email = application.Email,
            ResumeFileName = application.ResumeFileName, Resume = application.Resume, Source = application.Source,
            ReferralJobSiteId = application.ReferralJobSiteId
        };
        context.Applications.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.Id;
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
        var item = await context.Applications.AsNoTracking()
            .SingleOrDefaultAsync(item => item.PostingId == postingId && item.Id == applicationId, cancellationToken);
        return item is null ? null : new Application(item.Id, item.PostingId, item.Name, item.Email,
            item.ResumeFileName, item.Resume, item.Source, item.ReferralJobSiteId);
    }
}
