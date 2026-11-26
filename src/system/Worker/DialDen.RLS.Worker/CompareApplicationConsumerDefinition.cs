using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Worker;

public sealed class CompareApplicationConsumerDefinition : ConsumerDefinition<CompareApplicationConsumer>
{
    public CompareApplicationConsumerDefinition()
    {
        EndpointName = CompareApplication.QueueName;
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CompareApplicationConsumer> consumerConfigurator,
        IRegistrationContext context) => endpointConfigurator.PrefetchCount = 1;
}
