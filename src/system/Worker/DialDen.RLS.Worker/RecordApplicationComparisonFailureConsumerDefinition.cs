using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Worker;

public sealed class RecordApplicationComparisonFailureConsumerDefinition
    : ConsumerDefinition<RecordApplicationComparisonFailureConsumer>
{
    public RecordApplicationComparisonFailureConsumerDefinition() =>
        EndpointName = $"{CompareApplication.QueueName}-failures";
}
