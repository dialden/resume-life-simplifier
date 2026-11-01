using System.Runtime.Loader;
using DialDen.RLS.JobSites;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Worker.Root;

internal static class JobSiteDiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddDiscoveredJobSites(
        this IServiceCollection services, IConfiguration configuration)
    {
        var types = Directory.EnumerateFiles(AppContext.BaseDirectory, "DialDen.RLS.JobSites.*.dll")
            .Order(StringComparer.Ordinal)
            .Select(path => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path)))
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .ToArray();

        foreach (var pluginType in types.Where(type => typeof(IJobSitePlugin).IsAssignableFrom(type)))
        {
            var constructor = pluginType.GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"Job-site plugin '{pluginType.FullName}' requires a public parameterless constructor.");
            var plugin = (IJobSitePlugin)constructor.Invoke(null);
            plugin.ConfigureServices(services, configuration);
        }

        foreach (var jobSiteType in types.Where(type => typeof(IJobSite).IsAssignableFrom(type)))
        {
            services.AddTransient(typeof(IJobSite), jobSiteType);
        }
        return services;
    }
}
