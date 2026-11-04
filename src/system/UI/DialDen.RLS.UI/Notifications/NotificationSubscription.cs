using System.Threading.Channels;

namespace DialDen.RLS.UI.Notifications;

public sealed class NotificationSubscription : IDisposable
{
    private readonly Action _unsubscribe;
    private int _disposed;

    internal NotificationSubscription(ChannelReader<NotificationSignal> signals, Action unsubscribe)
    {
        Signals = signals;
        _unsubscribe = unsubscribe;
    }

    public ChannelReader<NotificationSignal> Signals { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _unsubscribe();
        }
    }
}
