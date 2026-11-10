using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.API.Notifications;

public sealed class BroadcastPostingRefreshConsumer(SseConnectionManager connections)
    : IConsumer<PostingChanged>
{
    public Task Consume(ConsumeContext<PostingChanged> context)
    {
        connections.Broadcast();
        return Task.CompletedTask;
    }
}
