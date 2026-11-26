using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Worker;

public sealed class CompareApplicationConsumer(ICompareApplication comparison)
    : IConsumer<CompareApplication>
{
    public async Task Consume(ConsumeContext<CompareApplication> context)
    {
        if (await comparison.Compare(context.Message.PostingId, context.Message.ApplicationId, context.CancellationToken))
            await context.Publish(new ApplicationChanged(context.Message.PostingId, context.Message.ApplicationId),
                context.CancellationToken);
    }
}
