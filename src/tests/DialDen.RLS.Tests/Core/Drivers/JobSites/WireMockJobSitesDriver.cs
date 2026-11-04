using WireMock.Admin.Mappings;
using WireMock.Client;

namespace DialDen.RLS.Tests.Core.Drivers.JobSites;

public sealed class WireMockJobSitesDriver(IWireMockAdminApi adminApi, IReadOnlyList<string> jobSiteIds) : IJobSitesDriver
{
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
