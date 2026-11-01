using DialDen.RLS.JobSites;

namespace DialDen.RLS.Worker.Root;

/// <summary>A test-only job site whose responses are provided by WireMock.</summary>
internal sealed class StubJobSite(HttpClient httpClient) : IJobSite
{
    public const string JobSiteId = "stub-job-site";
    public const string JobSiteName = "Stub Job Site";

    public string Id => JobSiteId;

    public string Name => JobSiteName;

    public async Task Publish(JobSitePosting posting, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            $"publish/{Uri.EscapeDataString(Id)}?postingId={posting.PostingId}",
            content: null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
