using DialDen.RLS.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DialDen.RLS.Worker;

public sealed class RecordInformationExtractionFailureConsumerDefinition
    : ConsumerDefinition<RecordInformationExtractionFailureConsumer>
{
    public RecordInformationExtractionFailureConsumerDefinition(IOptions<MessagingOptions> options)
    {
        EndpointName = $"{options.Value.ResumeExtractionQueue}-failures";
    }
}
