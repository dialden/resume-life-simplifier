using DialDen.RLS.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DialDen.RLS.Worker;

public sealed class ExtractApplicationInformationConsumerDefinition : ConsumerDefinition<ExtractApplicationInformationConsumer>
{
    public ExtractApplicationInformationConsumerDefinition(IOptions<MessagingOptions> options)
    {
        EndpointName = options.Value.ResumeExtractionQueue;
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ExtractApplicationInformationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.PrefetchCount = 1;
    }
}
