using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.API.Notifications;

public sealed class BroadcastApplicationRefreshConsumer(SseConnectionManager connections)
    : IConsumer<ApplicationChanged>
{
    public Task Consume(ConsumeContext<ApplicationChanged> context)
    {
        connections.Broadcast();
        return Task.CompletedTask;
    }
}
