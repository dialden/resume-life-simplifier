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
        var applications = await context.Applications.AsNoTracking().Where(item => item.PostingId == postingId)
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id, item.Name, item.Email, item.Source, item.ReferralJobSiteId, item.Status, item.ReviewVersion,
                item.ExtractedName, item.ExtractedEmail, item.ExtractedPhoneNumber, item.ExtractedPostalAddress,
                item.ExtractedLinkedInUrl,
                Education = item.EducationHistory.OrderBy(entry => entry.Position)
                    .Select(entry => new ExtractedEducation(entry.Institution, entry.Qualification,
                        entry.StartDate, entry.EndDate)).ToArray(),
                Assessment = item.EducationAssessment
            })
            .ToArrayAsync(cancellationToken);
        return applications.Select(item => new ApplicationSummary(
            item.Id, item.Name, item.Email, item.Source, item.ReferralJobSiteId,
            item.Status,
            item.Status is ApplicationStatus.AwaitingComparison or ApplicationStatus.ComparisonFailed or ApplicationStatus.Ready
                ? new ExtractedContactInformation(item.ExtractedName, item.ExtractedEmail, item.ExtractedPhoneNumber,
                    item.ExtractedPostalAddress, item.ExtractedLinkedInUrl) : null,
            item.Status is ApplicationStatus.AwaitingComparison or ApplicationStatus.ComparisonFailed or ApplicationStatus.Ready
                ? item.Education : null,
            Application.GroupFor(item.Status, item.Assessment), item.Assessment?.ToResponse(), item.ReviewVersion)).ToArray();
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
            when (exception.Entries.Any(conflict => ReferenceEquals(conflict.Entity, application)
                || ReferenceEquals(conflict.Entity, application.EducationAssessment)))
        {
            return false;
        }
    }
}
