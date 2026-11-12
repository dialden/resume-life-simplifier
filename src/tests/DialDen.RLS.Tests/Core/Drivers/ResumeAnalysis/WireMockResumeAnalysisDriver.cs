using System.Text.Json;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using WireMock.Admin.Mappings;
using WireMock.Client;

namespace DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;

public sealed class WireMockResumeAnalysisDriver(
    IWireMockAdminApi adminApi, Func<Task> restartWorker) : IResumeAnalysisDriver
{
    private bool _hasDelayedResponse;

    public async Task<int> AnalysisAttempts() =>
        (await adminApi.GetRequestsAsync()).Count(entry => entry.Request?.Path == "/api/chat");

    public async Task FailsAnalysis()
    {
        var restart = _hasDelayedResponse;
        await adminApi.ResetMappingsAsync();
        await adminApi.ResetRequestsAsync();
        await ConfigureIdentityMetadata();
        await adminApi.PostMappingAsync(new MappingModel
        {
            Request = new RequestModel { Path = "/api/chat", Methods = ["POST"] },
            Response = new ResponseModel { StatusCode = 503 }
        });
        if (restart)
            await restartWorker();
        _hasDelayedResponse = false;
    }

    public async Task ResetAndReturnNoContactsByDefault()
    {
        await FindsContactInformation(new(null, null, null, null, null), holdResponse: false);
    }

    public async Task FindsContactInformation(ExtractedContactInformation contacts, bool holdResponse)
    {
        var restart = _hasDelayedResponse && !holdResponse;
        await adminApi.ResetMappingsAsync();
        await adminApi.ResetRequestsAsync();
        await ConfigureIdentityMetadata();
        var body = JsonSerializer.Serialize(new
        {
            model = "qwen2.5vl:7b",
            created_at = "2026-01-01T00:00:00Z",
            message = new
            {
                role = "assistant",
                content = JsonSerializer.Serialize(contacts, JsonSerializerOptions.Web)
            },
            done = true,
            done_reason = "stop"
        });
        await adminApi.PostMappingAsync(new MappingModel
        {
            Request = new RequestModel
            {
                Path = "/api/chat",
                Methods = ["POST"]
            },
            Response = new ResponseModel
            {
                StatusCode = 200,
                // Exceeds the client's inference deadline; scenario cleanup cancels the request.
                Delay = holdResponse ? checked((int)TimeSpan.FromHours(1).TotalMilliseconds) : null,
                Headers = new Dictionary<string, object> { ["Content-Type"] = "application/json" },
                Body = body
            }
        });
        if (restart)
            await restartWorker();
        _hasDelayedResponse = holdResponse;
    }

    private Task ConfigureIdentityMetadata() =>
        adminApi.PostMappingAsync(new MappingModel
        {
            Request = new RequestModel
            {
                Path = "/computeMetadata/v1/instance/service-accounts/default/identity",
                Methods = ["GET"],
                Params =
                [
                    new ParamModel
                    {
                        Name = "audience",
                        Matchers = [new MatcherModel { Name = "ExactMatcher", Pattern = "https://test-resume-analysis.run.app" }]
                    }
                ],
                Headers =
                [
                    new HeaderModel
                    {
                        Name = "Metadata-Flavor",
                        Matchers = [new MatcherModel { Name = "ExactMatcher", Pattern = "Google" }]
                    }
                ]
            },
            Response = new ResponseModel
            {
                StatusCode = 200,
                Headers = new Dictionary<string, object> { ["Metadata-Flavor"] = "Google" },
                Body = "test-identity-token"
            }
        });
}
