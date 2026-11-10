using DialDen.RLS.Infrastructure.JobSites;
using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Infrastructure.Publishing;
using DialDen.RLS.Messaging.Contracts;
using DialDen.RLS.Postings;
using DialDen.RLS.Infrastructure.PublicWebsite;
using DialDen.RLS.Infrastructure.Applications;
using DialDen.RLS.JobApplications;
using MassTransit;
using MassTransit.SqlTransport.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DialDen.RLS.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    private static IServiceCollection AddRLSResumeExtraction(this IServiceCollection services)
    {
        services.AddOptions<OllamaOptions>().BindConfiguration(OllamaOptions.SectionName)
            .Validate(options => options.Endpoint is { IsAbsoluteUri: true }
                && options.Endpoint.Scheme is "http" or "https",
                "Ollama:Endpoint must be an absolute HTTP or HTTPS URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "Ollama:Model is required.");
        services.AddTransient<ResumePageImageRenderer>();
        services.AddHttpClient<IResumeContactExtraction, OllamaResumeContactExtraction>((provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<IOptions<OllamaOptions>>().Value.Endpoint;
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        return services;
    }

    /// <summary>
    /// Registers driven adapters and messaging. Transactional messaging enables consumer inbox/outbox,
    /// bounded retries and durable outbox delivery; otherwise consumers are nontransactional.
    /// </summary>
    public static IServiceCollection AddRLSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator> addConsumers,
        Action<InfrastructureOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(addConsumers);
        var infrastructure = new InfrastructureOptions();
        configure?.Invoke(infrastructure);
        var useConsumerOutbox = infrastructure.UseTransactionalMessaging;

        services.AddRLSResumeExtraction();

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
        services.AddScoped<IJobSitePublicationRequests, OutboxJobSitePublicationRequests>();
        services.AddScoped<IJobSitesCatalog, EntityFrameworkJobSitesCatalog>();
        services.AddScoped<IApplicationsArchive, EntityFrameworkApplicationsArchive>();
        services.AddScoped<IApplicationReceipts, OutboxApplicationReceipts>();
        services.AddSingleton<IPublicListingAddress>(serviceProvider =>
            new ConfiguredPublicListingAddress(
                serviceProvider.GetRequiredService<IConfiguration>()));
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
                    if (!useConsumerOutbox)
                    {
                        busOutbox.DisableDeliveryService();
                    }
                });
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
            });
            if (useConsumerOutbox)
            {
                bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
                {
                    // Retries our own transient failures, then moves the command to the error queue.
                    endpoint.UseMessageRetry(retry => retry.Intervals(
                        TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
                    endpoint.UseEntityFrameworkOutbox<RLSContext>(context);
                });
            }
            addConsumers(bus);
            bus.UsingPostgres((context, transport) =>
            {
                var messaging = context.GetRequiredService<IOptions<MessagingOptions>>().Value;
                transport.Message<ApplicationReceived>(message => message.SetEntityName(messaging.ApplicationReceivedTopic));
                transport.Message<Fault<ApplicationReceived>>(message =>
                    message.SetEntityName($"{messaging.ApplicationReceivedTopic}-fault"));
                var options = context.GetRequiredService<IOptions<SqlTransportOptions>>().Value;
                var connection = new NpgsqlConnectionStringBuilder(options.ConnectionString);
                if (connection.Host?.StartsWith("/", StringComparison.Ordinal) == true)
                {
                    // MassTransit needs a URI-safe identity, separate from Npgsql's Unix-socket path.
                    transport.Host(new PostgresSqlHostSettings(options) { Host = "localhost" });
                }
                transport.ConfigureEndpoints(context);
            });
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
