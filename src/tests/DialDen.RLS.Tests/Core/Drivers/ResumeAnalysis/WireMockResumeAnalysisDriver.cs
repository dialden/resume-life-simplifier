using System.Text.Json;
using DialDen.RLS.Tests.Core.Clients;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using WireMock.Admin.Mappings;

namespace DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;

public sealed class WireMockResumeAnalysisDriver(
    string callbackHost, WireMockMappings mappings) : IResumeAnalysisDriver, IAsyncDisposable
{
    private PendingResumeAnalysisResponse? _pendingResponse;

    public async Task FailsAnalysis()
    {
        await mappings.ResetAsync();
        await ConfigureIdentityMetadata();
        await mappings.PostAsync(new MappingModel
        {
            Request = new RequestModel { Path = mappings.PathFor("/api/chat"), Methods = ["POST"] },
            Response = new ResponseModel { StatusCode = 503 }
        });
        await CompletePendingResponse(503, "");
    }

    public async Task ResetAndReturnNoContactsByDefault()
    {
        await FindsContactInformation(new(null, null, null, null, null), holdResponse: false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_pendingResponse is not null)
            await ResetAndReturnNoContactsByDefault();
    }

    public async Task FindsContactInformation(ExtractedContactInformation contacts, bool holdResponse)
    {
        await mappings.ResetAsync();
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
        await CompletePendingResponse(200, body);
        var chatPath = mappings.PathFor("/api/chat");
        if (holdResponse)
        {
            _pendingResponse = new PendingResumeAnalysisResponse(chatPath);
            var proxyUrl = await _pendingResponse.Start(callbackHost);
            await mappings.PostAsync(new MappingModel
            {
                Request = new RequestModel { Path = chatPath, Methods = ["POST"] },
                Response = new ResponseModel { ProxyUrl = proxyUrl }
            });
            return;
        }
        await mappings.PostAsync(new MappingModel
        {
            Request = new RequestModel
            {
                Path = chatPath,
                Methods = ["POST"]
            },
            Response = new ResponseModel
            {
                StatusCode = 200,
                Headers = new Dictionary<string, object> { ["Content-Type"] = "application/json" },
                Body = body
            }
        });
    }

    private async Task CompletePendingResponse(int statusCode, string body)
    {
        if (_pendingResponse is null)
            return;
        _pendingResponse.Complete(statusCode, body);
        await _pendingResponse.DisposeAsync();
        _pendingResponse = null;
    }

    private Task ConfigureIdentityMetadata() =>
        mappings.PostAsync(new MappingModel
        {
            Request = new RequestModel
            {
                Path = mappings.PathFor("/computeMetadata/v1/instance/service-accounts/default/identity"),
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
