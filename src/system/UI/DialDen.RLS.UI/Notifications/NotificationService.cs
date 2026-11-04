using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.UI.Notifications;

public sealed class NotificationService(
    HttpClient api,
    NotificationStreamClient stream,
    ILogger<NotificationService> logger)
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Channel<NotificationSignal>> _subscribers = [];
    private NotificationSignal? _latest;
    private int _running;

    public NotificationSubscription Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<NotificationSignal>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            // Signals request a REST refresh, so pending refreshes can coalesce.
            FullMode = BoundedChannelFullMode.DropOldest
        });
        lock (_gate)
        {
            _subscribers.Add(id, channel);
            if (_latest is { } latest)
            {
                channel.Writer.TryWrite(latest);
            }
        }
        return new NotificationSubscription(channel.Reader, () =>
        {
            lock (_gate)
            {
                if (_subscribers.Remove(id, out var removed))
                {
                    removed.Writer.TryComplete();
                }
            }
        });
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException("The shared notification stream is already running.");
        }
        try
        {
            var entryPoint = await api.GetFromJsonAsync<RLSEntryPoint>("/", cancellationToken)
                ?? throw new JsonException("The API returned no entry point.");
            var link = entryPoint.Links.SingleOrDefault(
                link => link is { Rel: "notifications", Method: "GET" })
                ?? throw new InvalidOperationException("The API does not advertise a notification stream.");

            await foreach (var signal in stream.ReadAsync(link.Href, cancellationToken))
            {
                Broadcast(signal);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The application shell owns the connection lifetime.
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException
            or JsonException or InvalidOperationException or OperationCanceledException)
        {
            logger.LogWarning(exception, "The shared notification stream stopped.");
            Broadcast(NotificationSignal.Unavailable);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    private void Broadcast(NotificationSignal signal)
    {
        lock (_gate)
        {
            _latest = signal;
            foreach (var channel in _subscribers.Values)
            {
                channel.Writer.TryWrite(signal);
            }
        }
    }
}
