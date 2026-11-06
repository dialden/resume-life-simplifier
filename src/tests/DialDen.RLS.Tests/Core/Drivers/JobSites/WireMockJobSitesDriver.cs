using WireMock.Admin.Mappings;
using WireMock.Client;
using System.Globalization;

namespace DialDen.RLS.Tests.Core.Drivers.JobSites;

public sealed class WireMockJobSitesDriver(IWireMockAdminApi adminApi, IReadOnlyList<string> jobSiteIds) : IJobSitesDriver
{
    public async Task<IReadOnlyList<Uri?>> PublicListingAddressesReceivedFor(string jobSiteId, int postingId)
    {
        var requests = await adminApi.GetRequestsAsync();
        var addresses = new List<Uri?>();
        foreach (var entry in requests)
        {
            var request = entry.Request
                ?? throw new InvalidOperationException("The job-site request log contains no request.");
            if (request.Path != $"/publish/{Uri.EscapeDataString(jobSiteId)}"
                || request.Query is null
                || !request.Query.TryGetValue("postingId", out var ids)
                || !ids.Contains(postingId.ToString(CultureInfo.InvariantCulture)))
            {
                continue;
            }
            if (!request.Query.TryGetValue("publicListingUrl", out var urls))
            {
                addresses.Add(null);
            }
            else
            {
                addresses.AddRange(urls.Select(address => new Uri(address, UriKind.Absolute)));
            }
        }
        return addresses;
    }

    public async Task ResetAndAcceptByDefault()
    {
        await adminApi.ResetMappingsAsync();
        await adminApi.ResetRequestsAsync();
        foreach (var jobSiteId in jobSiteIds)
        {
            await PostMapping(jobSiteId, postingId: ".*", accepts: true, matcher: "RegexMatcher");
        }
    }

    public Task SetResponse(string jobSiteId, int postingId, bool accepts, TimeSpan delay) =>
        PostMapping(jobSiteId, postingId.ToString(System.Globalization.CultureInfo.InvariantCulture), accepts,
            delay: delay);

    private Task PostMapping(
        string jobSiteId, string postingId, bool accepts, string matcher = "ExactMatcher", TimeSpan delay = default) =>
        adminApi.PostMappingAsync(new MappingModel
        {
            Request = new RequestModel
            {
                Path = $"/publish/{Uri.EscapeDataString(jobSiteId)}",
                Methods = ["POST"],
                Params =
                [
                    new ParamModel
                    {
                        Name = "postingId",
                        Matchers = [new MatcherModel { Name = matcher, Pattern = postingId }]
                    }
                ]
            },
            Response = new ResponseModel
            {
                StatusCode = accepts ? 204 : 422,
                Delay = delay > TimeSpan.Zero ? (int)delay.TotalMilliseconds : null
            }
        });
}
