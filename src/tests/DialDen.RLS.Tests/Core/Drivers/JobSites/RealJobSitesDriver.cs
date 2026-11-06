namespace DialDen.RLS.Tests.Core.Drivers.JobSites;

public sealed class RealJobSitesDriver : IJobSitesDriver
{
    public Task<IReadOnlyList<Uri?>> PublicListingAddressesReceivedFor(string jobSiteId, int postingId) =>
        throw new InvalidOperationException(
            "Inspecting job-site requests requires the stubbed external systems environment.");

    // Real job sites are not reset or given a default response by the test harness.
    public Task ResetAndAcceptByDefault() => Task.CompletedTask;

    public Task SetResponse(string jobSiteId, int postingId, bool accepts, TimeSpan delay) =>
        throw new InvalidOperationException(
            "Controlling job-site responses requires the stubbed external systems environment.");
}
