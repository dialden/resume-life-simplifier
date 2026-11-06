namespace DialDen.RLS.Tests.Core.Drivers.JobSites;

/// <summary>Controls the WireMock responses for the test job-site provider.</summary>
public interface IJobSitesDriver
{
    Task ResetAndAcceptByDefault();

    Task SetResponse(string jobSiteId, int postingId, bool accepts, TimeSpan delay);

    Task<IReadOnlyList<Uri?>> PublicListingAddressesReceivedFor(string jobSiteId, int postingId);
}
