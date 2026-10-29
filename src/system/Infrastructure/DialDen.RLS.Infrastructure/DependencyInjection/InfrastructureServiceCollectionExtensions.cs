using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Postings;
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
        return services;
    }
}
