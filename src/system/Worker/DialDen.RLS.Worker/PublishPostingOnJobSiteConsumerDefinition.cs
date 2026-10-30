using DialDen.RLS.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DialDen.RLS.Worker;

public sealed class PublishPostingOnJobSiteConsumerDefinition : ConsumerDefinition<PublishPostingOnJobSiteConsumer>
{
    public PublishPostingOnJobSiteConsumerDefinition(IOptions<MessagingOptions> options)
    {
        EndpointName = options.Value.JobSitePublicationQueue;
    }
}
