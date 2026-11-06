using DialDen.RLS.JobSites;

namespace DialDen.RLS.Worker.Root;

/// <summary>A test-only job site whose responses are provided by WireMock.</summary>
internal sealed class StubJobSite(string id, string name, HttpClient httpClient) : IJobSite, IDisposable
{
    public string Id => id;

    public string Name => name;

    public async Task Publish(JobSitePosting posting, CancellationToken cancellationToken)
    {
        var publicListingQuery = posting.PublicListingUrl is null
            ? ""
            : $"&publicListingUrl={Uri.EscapeDataString(posting.PublicListingUrl.AbsoluteUri)}";
        using var response = await httpClient.PostAsync(
            $"publish/{Uri.EscapeDataString(Id)}?postingId={posting.PostingId}{publicListingQuery}",
            content: null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => httpClient.Dispose();
}
