using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.Worker;

public sealed class RecordInformationExtractionFailureConsumer(
    IRecordInformationExtractionFailure failure, ILogger<RecordInformationExtractionFailureConsumer> logger)
    : IConsumer<Fault<ApplicationReceived>>
{
    public async Task Consume(ConsumeContext<Fault<ApplicationReceived>> context)
    {
        var application = context.Message.Message;
        if (await failure.Record(application.PostingId, application.ApplicationId, context.CancellationToken))
        {
            logger.LogWarning(
                "Information extraction failed after retries for application {ApplicationId} on posting {PostingId}.",
                application.ApplicationId, application.PostingId);
            await context.Publish(
                new ApplicationChanged(application.PostingId, application.ApplicationId),
                context.CancellationToken);
        }
    }
}
