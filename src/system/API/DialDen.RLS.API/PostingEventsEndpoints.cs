using System.Globalization;
using DialDen.RLS.Abstractions.Postings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace DialDen.RLS.API;

/// <summary>Streams server-sent events telling clients when to read a posting again.</summary>
public static class PostingEventsEndpoints
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    public static IEndpointRouteBuilder MapPostingEvents(this IEndpointRouteBuilder app)
    {
        app.MapGet("/postings/{id:int}/events", StreamPostingEvents)
            .WithName("PostingEvents")
            .WithSummary("Streams notifications about changes to a job posting.")
            .WithDescription("A text/event-stream. A ready event is sent once connected: read the posting then. Comment lines are heartbeats keeping the connection open.")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<EmptyHttpResult, NotFound>> StreamPostingEvents(
        int id,
        HttpContext httpContext,
        [FromServices] IViewPosting viewPosting,
        [FromServices] IHostApplicationLifetime lifetime,
        CancellationToken cancellationToken)
    {
        if (await viewPosting.View(new ViewPostingRequest(id), cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        // Streams stay open until the client leaves, so they must also end when the API stops.
        using var streaming = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, lifetime.ApplicationStopping);
        httpContext.Features.GetRequiredFeature<IHttpResponseBodyFeature>().DisableBuffering();
        var response = httpContext.Response;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        // Asks nginx-style reverse proxies not to buffer the stream.
        response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await Write(response, Event("ready", id), streaming.Token);
            using var heartbeat = new PeriodicTimer(HeartbeatInterval);
            while (await heartbeat.WaitForNextTickAsync(streaming.Token))
            {
                await Write(response, ": heartbeat\n\n", streaming.Token);
            }
        }
        catch (OperationCanceledException) when (streaming.IsCancellationRequested)
        {
        }

        return TypedResults.Empty;
    }

    private static string Event(string name, int postingId) =>
        $"event: {name}\ndata: {{\"postingId\":{postingId.ToString(CultureInfo.InvariantCulture)}}}\n\n";

    private static async Task Write(HttpResponse response, string frame, CancellationToken cancellationToken)
    {
        await response.WriteAsync(frame, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
