using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.Worker;

public sealed class RecordApplicationComparisonFailureConsumer(
    ICompareApplication comparison, ILogger<RecordApplicationComparisonFailureConsumer> logger)
    : IConsumer<Fault<CompareApplication>>
{
    public async Task Consume(ConsumeContext<Fault<CompareApplication>> context)
    {
        var message = context.Message.Message;
        if (await comparison.RecordFailure(message.PostingId, message.ApplicationId, context.CancellationToken))
        {
            logger.LogWarning("Application comparison failed after retries for application {ApplicationId} on posting {PostingId}.",
                message.ApplicationId, message.PostingId);
            await context.Publish(new ApplicationChanged(message.PostingId, message.ApplicationId),
                context.CancellationToken);
        }
    }
}
