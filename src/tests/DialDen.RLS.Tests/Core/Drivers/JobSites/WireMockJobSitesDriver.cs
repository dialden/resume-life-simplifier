using WireMock.Admin.Mappings;
using WireMock.Client;

namespace DialDen.RLS.Tests.Core.Drivers.JobSites;

public sealed class WireMockJobSitesDriver(IWireMockAdminApi adminApi) : IJobSitesDriver
{
    public async Task ResetAndAcceptByDefault()
    {
        await adminApi.ResetMappingsAsync();
        await adminApi.ResetRequestsAsync();
        await PostMapping(
            "stub-job-site",
            postingId: ".*",
            accepts: true,
            matcher: "RegexMatcher");
    }

    public Task SetResponse(string jobSiteId, int postingId, bool accepts) =>
        PostMapping(jobSiteId, postingId.ToString(System.Globalization.CultureInfo.InvariantCulture), accepts);

    private Task PostMapping(string jobSiteId, string postingId, bool accepts, string matcher = "ExactMatcher") =>
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
            Response = new ResponseModel { StatusCode = accepts ? 204 : 422 }
        });
}
