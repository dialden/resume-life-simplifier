using DialDen.RLS.JobSites;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Worker.Root;

public static class JobSitesServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredJobSites(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(JobSitesOptions.SectionName).Get<JobSitesOptions>()
            ?? new JobSitesOptions();
        switch (options.Type)
        {
            case "real":
                return services.AddDiscoveredJobSites(configuration);
            case "stubbed":
                if (options.Sites.Count == 0)
                {
                    throw new InvalidOperationException("JobSites:Sites must contain at least one stubbed job site.");
                }
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var site in options.Sites)
                {
                    if (string.IsNullOrWhiteSpace(site.Id) || string.IsNullOrWhiteSpace(site.Name))
                    {
                        throw new InvalidOperationException("Each stubbed job site requires an Id and Name.");
                    }
                    if (!ids.Add(site.Id))
                    {
                        throw new InvalidOperationException($"Several stubbed job sites are identified by '{site.Id}'.");
                    }
                    if (!Uri.TryCreate(site.Url, UriKind.Absolute, out var address)
                        || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
                        || !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
                    {
                        throw new InvalidOperationException(
                            $"JobSites:Sites URL for '{site.Id}' must be an absolute HTTP(S) base URL without a query or fragment.");
                    }
                    var clientName = $"job-site:{site.Id}";
                    var id = site.Id;
                    var name = site.Name;
                    services.AddHttpClient(clientName, client =>
                        client.BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/"));
                    services.AddTransient<IJobSite>(provider => new StubJobSite(
                        id, name, provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName)));
                }
                return services;
            default:
                throw new InvalidOperationException($"Unsupported JobSites:Type: '{options.Type}'.");
        }
    }
}
