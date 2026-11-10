using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Worker;

public sealed class ExtractApplicationInformationConsumer(IExtractApplicationInformation extractApplicationInformation)
    : IConsumer<ApplicationReceived>
{
    public async Task Consume(ConsumeContext<ApplicationReceived> context)
    {
        if (await extractApplicationInformation.Extract(
                context.Message.PostingId, context.Message.ApplicationId, context.CancellationToken))
        {
            await context.Publish(
                new ApplicationChanged(context.Message.PostingId, context.Message.ApplicationId),
                context.CancellationToken);
        }
    }
}
