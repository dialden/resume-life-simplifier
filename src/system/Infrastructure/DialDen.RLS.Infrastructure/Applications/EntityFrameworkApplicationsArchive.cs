using DialDen.RLS.JobApplications;
using DialDen.RLS.Abstractions.Applications;
using Microsoft.EntityFrameworkCore;

namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class EntityFrameworkApplicationsArchive(RLSContext context) : IApplicationsArchive
{
    public Task<int> NextId(CancellationToken cancellationToken) =>
        context.Database.SqlQuery<int>(
            $"""SELECT nextval(pg_get_serial_sequence('applications', 'id'))::integer AS "Value" """)
            .SingleAsync(cancellationToken);

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
            .Select(item => new ApplicationSummary(
                item.Id, item.Name, item.Email, item.Source, item.ReferralJobSiteId,
                item.ExtractionStatus,
                item.ExtractionStatus == InformationExtractionStatus.Completed
                    ? new ExtractedContactInformation(
                        item.ExtractedName,
                        item.ExtractedEmail,
                        item.ExtractedPhoneNumber,
                        item.ExtractedPostalAddress,
                        item.ExtractedLinkedInUrl)
                    : null))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Application?> Find(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        return await context.Applications
            .Where(item => item.PostingId == postingId && item.Id == applicationId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> Save(Application application, CancellationToken cancellationToken)
    {
        var entry = context.Entry(application);
        if (entry.State == EntityState.Detached)
            throw new InvalidOperationException("Only a tracked application can be saved.");

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException exception)
            when (exception.Entries.Any(conflict => ReferenceEquals(conflict.Entity, application)))
        {
            return false;
        }
    }
}
