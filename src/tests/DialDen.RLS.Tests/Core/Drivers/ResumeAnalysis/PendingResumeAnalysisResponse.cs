using System.Diagnostics;
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
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private readonly ITestOutputHelper? _output = TestContext.Current.TestOutputHelper;
    private int _requests;
    private int _abortedRequests;
    private long _lastAbortMilliseconds = -1;

    public PendingResumeAnalysisResponse(string chatPath)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options => options.ListenAnyIP(0));
        _host = builder.Build();
        _host.MapPost(chatPath, async context =>
        {
            Interlocked.Increment(ref _requests);
            try
            {
                var response = await _response.Task.WaitAsync(context.RequestAborted);
                context.Response.StatusCode = response.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(response.Body, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                Interlocked.Increment(ref _abortedRequests);
                Interlocked.Exchange(ref _lastAbortMilliseconds, _elapsed.ElapsedMilliseconds);
                throw;
            }
        });
    }

    public async Task<string> Start(string callbackHost)
    {
        await _host.StartAsync();
        var address = _host.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The pending resume-analysis host has no listening address.");
        var callback = new UriBuilder(address) { Host = callbackHost }.Uri;
        _output?.WriteLine($"Held resume-analysis callback listening at {callback.Host}:{callback.Port}.");
        // WireMock appends the incoming path, including its leading slash.
        return callback.GetLeftPart(UriPartial.Authority);
    }

    public void Complete(int statusCode, string body)
    {
        _output?.WriteLine($"Releasing held resume analysis after {_elapsed.ElapsedMilliseconds} ms (HTTP {statusCode}).");
        _response.SetResult((statusCode, body));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _host.StopAsync();
            await _host.DisposeAsync();
        }
        finally
        {
            var lastAbort = Interlocked.Read(ref _lastAbortMilliseconds);
            _output?.WriteLine(
                $"Held resume analysis: {Volatile.Read(ref _requests)} requests, " +
                $"{Volatile.Read(ref _abortedRequests)} aborted" +
                (lastAbort >= 0 ? $" (last abort at {lastAbort} ms)." : "."));
        }
    }
}
