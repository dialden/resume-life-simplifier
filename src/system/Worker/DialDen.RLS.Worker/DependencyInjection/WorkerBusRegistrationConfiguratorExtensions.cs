using MassTransit;

namespace DialDen.RLS.Worker.DependencyInjection;

public static class WorkerBusRegistrationConfiguratorExtensions
{
    public static IBusRegistrationConfigurator AddRLSConsumers(this IBusRegistrationConfigurator bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        bus.AddConsumer<PublishPostingOnJobSiteConsumer, PublishPostingOnJobSiteConsumerDefinition>();
        bus.AddConsumer<ExtractApplicationInformationConsumer, ExtractApplicationInformationConsumerDefinition>();
        bus.AddConsumer<RecordInformationExtractionFailureConsumer, RecordInformationExtractionFailureConsumerDefinition>();
        bus.AddConsumer<CompareApplicationConsumer, CompareApplicationConsumerDefinition>();
        bus.AddConsumer<RecordApplicationComparisonFailureConsumer, RecordApplicationComparisonFailureConsumerDefinition>();

        return bus;
    }
}
