using System.Threading.Channels;

namespace DialDen.RLS.API.Notifications;

/// <summary>Owns one connection's refresh hints; disposing unregisters it and completes its channel.</summary>
public sealed class SseSubscription : IDisposable
{
    private readonly Action _unsubscribe;
    private int _disposed;

    internal SseSubscription(ChannelReader<byte> changes, Action unsubscribe)
    {
        Changes = changes;
        _unsubscribe = unsubscribe;
    }

    public ChannelReader<byte> Changes { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _unsubscribe();
        }
    }
}
