using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Worker;

public sealed class PublishPostingOnJobSiteConsumer(IPublishOnJobSite publishOnJobSite)
    : IConsumer<PublishPostingOnJobSite>
{
    public async Task Consume(ConsumeContext<PublishPostingOnJobSite> context)
    {
        await publishOnJobSite.Publish(
            new PublishOnJobSiteRequest(context.Message.PostingId, context.Message.JobSiteId),
            context.CancellationToken);

        await context.Publish(
            new PostingChanged(context.Message.PostingId),
            context.CancellationToken);
    }
}
