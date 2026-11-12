using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;

public sealed class PendingResumeAnalysisResponse : IAsyncDisposable
{
    private readonly TaskCompletionSource<(int StatusCode, string Body)> _response =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly WebApplication _host;

    public PendingResumeAnalysisResponse()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options => options.ListenAnyIP(0));
        _host = builder.Build();
        _host.MapPost("/api/chat", async context =>
        {
            var response = await _response.Task.WaitAsync(context.RequestAborted);
            context.Response.StatusCode = response.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(response.Body, context.RequestAborted);
        });
    }

    public async Task<string> Start(string callbackHost)
    {
        await _host.StartAsync();
        var address = _host.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The pending resume-analysis host has no listening address.");
        return new UriBuilder(address) { Host = callbackHost }.Uri.ToString();
    }

    public void Complete(int statusCode, string body) =>
        _response.SetResult((statusCode, body));

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        await _host.DisposeAsync();
    }
}
