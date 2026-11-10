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

        return bus;
    }
}
