namespace DialDen.RLS.JobSites;

/// <summary>An external job site on which postings can be published. Each job-site plugin implements it.</summary>
public interface IJobSite
{
    /// <summary>Stable identifier, used to select the job site when publishing a posting.</summary>
    string Id { get; }

    string Name { get; }

    /// <summary>Publishes the posting on the job site. Throws when the job site does not accept it.</summary>
    Task Publish(JobSitePosting posting, CancellationToken cancellationToken);
}
