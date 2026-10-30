namespace DialDen.RLS.Abstractions.Postings;

public interface IJobSites
{
    /// <summary>Lists the job sites on which postings can currently be published.</summary>
    Task<IReadOnlyList<JobSite>> List(CancellationToken cancellationToken);
}
