namespace DialDen.RLS.Tests.Core.Drivers.JobSites;

/// <summary>Controls how external job sites answer the postings published to them.</summary>
public interface IJobSitesDriver
{
    void AcceptPostings(string jobSiteId, Func<int, bool> isConcernedPosting);

    void RejectPostings(string jobSiteId, Func<int, bool> isConcernedPosting);
}
