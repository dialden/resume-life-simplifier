using MassTransit;

namespace DialDen.RLS.Worker.DependencyInjection;

public static class WorkerBusRegistrationConfiguratorExtensions
{
    public static IBusRegistrationConfigurator AddRLSConsumers(this IBusRegistrationConfigurator bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        bus.AddConsumer<PublishPostingOnJobSiteConsumer, PublishPostingOnJobSiteConsumerDefinition>();

        return bus;
    }
}
