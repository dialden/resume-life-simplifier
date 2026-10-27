using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Postings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DialDen.RLS.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRLSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<NpgsqlDataSource>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("RLS")
                ?? throw new InvalidOperationException(
                    "The ConnectionStrings:RLS setting is required.");

            return NpgsqlDataSource.Create(connectionString);
        });
        services.AddSingleton<IPostingsArchive, PostgresJobPostings>();
        return services;
    }
}
