using System.Threading.Channels;

namespace DialDen.RLS.API.Notifications;

/// <summary>Broadcasts refresh hints to subscriptions in this API process only.</summary>
public sealed class SseConnectionManager
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Channel<byte>> _subscriptions = [];

    public SseSubscription Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            // Every hint means "refresh current data", so repeated pending hints can coalesce.
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_gate)
        {
            _subscriptions.Add(id, channel);
        }

        return new SseSubscription(channel.Reader, () => Remove(id));
    }

    public void Broadcast()
    {
        lock (_gate)
        {
            foreach (var channel in _subscriptions.Values)
            {
                channel.Writer.TryWrite(0);
            }
        }
    }

    private void Remove(Guid id)
    {
        lock (_gate)
        {
            if (_subscriptions.Remove(id, out var channel))
            {
                channel.Writer.TryComplete();
            }
        }
    }
}
