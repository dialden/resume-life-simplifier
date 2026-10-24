using DialDen.RLS.Infrastructure.Postings;
using DialDen.RLS.Postings;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DialDen.RLS.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRLSInfrastructure(
        this IServiceCollection services,
        string postingsConnectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(postingsConnectionString);

        services.AddSingleton(NpgsqlDataSource.Create(postingsConnectionString));
        services.AddSingleton<IPostingsArchive, PostgresPostingsArchive>();
        return services;
    }
}
