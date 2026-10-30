using DialDen.RLS.Infrastructure.JobSites;
using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Infrastructure.Publishing;
using DialDen.RLS.Messaging.Contracts;
using DialDen.RLS.Postings;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the infrastructure of a process that only requests work from others: its commands are
    /// written to the outbox, and a process consuming them delivers them.
    /// </summary>
    public static IServiceCollection AddRLSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddRLSInfrastructure(configuration, addConsumers: null);

    /// <summary>
    /// Registers the infrastructure of a process consuming commands. It also delivers the outbox commands.
    /// Each consumed command is processed at most once, in the same transaction as the changes it makes.
    /// </summary>
    public static IServiceCollection AddRLSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? addConsumers)
    {
        services.AddDbContext<RLSContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("RLS")
                ?? throw new InvalidOperationException(
                    "The ConnectionStrings:RLS setting is required.");

            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IPostingsArchive, EntityFrameworkPostingsArchive>();
        services.AddScoped<IJobSitePublications, EntityFrameworkJobSitePublications>();
        services.AddScoped<IJobSitesCatalog, EntityFrameworkJobSitesCatalog>();
        services.AddOptions<MessagingOptions>().BindConfiguration(MessagingOptions.SectionName);
        services.AddOptions<SqlTransportOptions>()
            .Configure<IConfiguration>((options, settings) =>
            {
                options.ConnectionString = settings.GetConnectionString("RLS")
                    ?? throw new InvalidOperationException("The ConnectionStrings:RLS setting is required.");
                options.Schema = "transport";
            });
        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<RLSContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox(busOutbox =>
                {
                    if (addConsumers is null)
                    {
                        busOutbox.DisableDeliveryService();
                    }
                });
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
            });
            if (addConsumers is not null)
            {
                bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
                {
                    // Retries our own transient failures, then moves the command to the error queue.
                    endpoint.UseMessageRetry(retry => retry.Intervals(
                        TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
                    endpoint.UseEntityFrameworkOutbox<RLSContext>(context);
                });
                addConsumers(bus);
            }
            bus.UsingPostgres((context, transport) => transport.ConfigureEndpoints(context));
        });
        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = TimeSpan.FromSeconds(30);
            options.StopTimeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
