using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Infrastructure.Publishing;
using DialDen.RLS.Postings;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRLSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
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
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
            });
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
