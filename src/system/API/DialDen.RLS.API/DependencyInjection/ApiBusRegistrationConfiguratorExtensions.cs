using DialDen.RLS.API.Notifications;
using MassTransit;

namespace DialDen.RLS.API.DependencyInjection;

public static class ApiBusRegistrationConfiguratorExtensions
{
    public static IBusRegistrationConfigurator AddRLSNotificationConsumers(
    this IBusRegistrationConfigurator bus)
    {
    var endpointName = $"rls-api-notifications-{Guid.NewGuid():N}";

        bus.AddConsumer<BroadcastPostingRefreshConsumer>()
            .Endpoint(endpoint =>
            {
                endpoint.Name = endpointName;
                endpoint.Temporary = true;
            });

        bus.AddConsumer<BroadcastApplicationRefreshConsumer>()
            .Endpoint(endpoint =>
            {
                endpoint.Name = endpointName;
                endpoint.Temporary = true;
            });

        return bus;
    }
}
