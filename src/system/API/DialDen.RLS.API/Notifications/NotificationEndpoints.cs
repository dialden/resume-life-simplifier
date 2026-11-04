using System.Net.Http.Headers;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.API.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotifications(this IEndpointRouteBuilder app)
    {
        app.MapGet("/notifications/stream", Stream)
            .WithName("NotificationStream")
            .WithSummary("Streams refresh hints broadcast by this API instance.")
            .WithDescription("Sends ready on connection and notifications-changed on a local broadcast. Clients reread current data through REST. Comment frames are heartbeats.")
            .Produces(StatusCodes.Status200OK, contentType: MediaTypeNames.Text.EventStream);

        return app;
    }

    private static async Task Stream(
        HttpContext context,
        SseConnectionManager connections,
        IHostApplicationLifetime lifetime,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var streaming = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, lifetime.ApplicationStopping);
        using var subscription = connections.Subscribe();
        var logger = loggerFactory.CreateLogger(nameof(NotificationEndpoints));
        var response = context.Response;
        context.Features.GetRequiredFeature<IHttpResponseBodyFeature>().DisableBuffering();
        response.ContentType = MediaTypeNames.Text.EventStream;
        var cacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoTransform = true
        };
        response.Headers.CacheControl = cacheControl.ToString();
        response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await Write(response, "event: ready\ndata: {}\n\n", streaming.Token);

            while (!streaming.IsCancellationRequested)
            {
                using var idle = CancellationTokenSource.CreateLinkedTokenSource(streaming.Token);
                idle.CancelAfter(TimeSpan.FromSeconds(15));

                bool available;
                try
                {
                    available = await subscription.Changes.WaitToReadAsync(idle.Token);
                }
                catch (OperationCanceledException) when (
                    idle.IsCancellationRequested && !streaming.IsCancellationRequested)
                {
                    await Write(response, ": heartbeat\n\n", streaming.Token);
                    continue;
                }

                if (!available)
                {
                    break;
                }

                if (subscription.Changes.TryRead(out _))
                {
                    await Write(response, "event: notifications-changed\ndata: {}\n\n", streaming.Token);
                }
            }
        }
        catch (OperationCanceledException) when (streaming.IsCancellationRequested)
        {
            logger.LogDebug("Notification stream closed.");
        }
        catch (IOException exception)
        {
            logger.LogDebug(exception, "Notification stream connection failed.");
            context.Abort();
        }
    }

    private static async Task Write(HttpResponse response, string frame, CancellationToken cancellationToken)
    {
        await response.WriteAsync(frame, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
