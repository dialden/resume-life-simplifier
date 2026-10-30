namespace DialDen.RLS.Tests.Core.Drivers.JobSites.Stub;

public class JobSitesStubDriver : IJobSitesDriver
{
    private readonly JobSitesStub _stub;

    public JobSitesStubDriver(JobSitesStub stub)
    {
        _stub = stub;
    }

    public void AcceptPostings(string jobSiteId, Func<int, bool> isConcernedPosting) =>
        _stub.AcceptPostings(jobSiteId, isConcernedPosting);

    public void RejectPostings(string jobSiteId, Func<int, bool> isConcernedPosting) =>
        _stub.RejectPostings(jobSiteId, isConcernedPosting);
}
