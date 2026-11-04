using DialDen.RLS.API.Notifications;
using MassTransit;

namespace DialDen.RLS.API.DependencyInjection;

public static class ApiBusRegistrationConfiguratorExtensions
{
    public static IBusRegistrationConfigurator AddRLSNotificationConsumers(
        this IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<PostingChangedConsumer>()
            .Endpoint(endpoint =>
            {
                endpoint.Name = $"rls-api-notifications-{Guid.NewGuid():N}";
                endpoint.Temporary = true;
            });

        return bus;
    }
}
