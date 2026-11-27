using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DialDen.RLS.Worker;

public sealed class ExtractApplicationInformationConsumer(
    IExtractApplicationInformation extractApplicationInformation, IApplications applications,
    ILogger<ExtractApplicationInformationConsumer> logger)
    : IConsumer<ApplicationReceived>
{
    public async Task Consume(ConsumeContext<ApplicationReceived> context)
    {
        var attempt = context.GetRetryAttempt();
        var elapsed = Stopwatch.StartNew();
        logger.LogInformation(new EventId(1001), "Extraction attempt {Attempt} started, message {MessageId}.",
            attempt, context.MessageId);
        try
        {
            await Extract(context);
            logger.LogInformation(new EventId(1002),
                "Extraction attempt {Attempt} completed, message {MessageId}, elapsed {ElapsedMs} ms.",
                attempt, context.MessageId, elapsed.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.LogInformation(new EventId(1003),
                "Extraction attempt {Attempt} faulted, message {MessageId}, elapsed {ElapsedMs} ms, exception {ExceptionType}.",
                attempt, context.MessageId, elapsed.ElapsedMilliseconds, exception.GetType().FullName);
            throw;
        }
    }

    private async Task Extract(ConsumeContext<ApplicationReceived> context)
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
