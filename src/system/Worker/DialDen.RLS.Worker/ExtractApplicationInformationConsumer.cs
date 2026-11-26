using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Worker;

public sealed class ExtractApplicationInformationConsumer(
    IExtractApplicationInformation extractApplicationInformation, IApplications applications)
    : IConsumer<ApplicationReceived>
{
    public async Task Consume(ConsumeContext<ApplicationReceived> context)
    {
        if (await extractApplicationInformation.Extract(
                context.Message.PostingId, context.Message.ApplicationId, context.CancellationToken))
        {
            var application = await applications.Find(
                context.Message.PostingId, context.Message.ApplicationId, context.CancellationToken)
                ?? throw new InvalidOperationException("The extracted application was not found.");
            if (application.Status == ApplicationStatus.AwaitingComparison)
            {
                var endpoint = await context.GetSendEndpoint(new Uri($"queue:{CompareApplication.QueueName}"));
                await endpoint.Send(new CompareApplication(context.Message.PostingId, context.Message.ApplicationId),
                    context.CancellationToken);
            }
            await context.Publish(
                new ApplicationChanged(context.Message.PostingId, context.Message.ApplicationId),
                context.CancellationToken);
        }
    }
}
