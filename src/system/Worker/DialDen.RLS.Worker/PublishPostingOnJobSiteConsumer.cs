using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Worker;

public sealed class PublishPostingOnJobSiteConsumer(IPublishOnJobSite publishOnJobSite)
    : IConsumer<PublishPostingOnJobSite>
{
    public Task Consume(ConsumeContext<PublishPostingOnJobSite> context) =>
        publishOnJobSite.Publish(
            new PublishOnJobSiteRequest(context.Message.PostingId, context.Message.JobSiteId),
            context.CancellationToken);
}
