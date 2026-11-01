using DialDen.RLS.JobSites;
using DialDen.RLS.JobSites.Jobillico.DependencyInjection;
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
                return services.AddJobillico();
            case "stubbed":
                if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var address)
                    || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
                    || !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
                {
                    throw new InvalidOperationException(
                        "JobSites:Url must be an absolute HTTP(S) base URL without a query or fragment.");
                }

                services.AddHttpClient<IJobSite, StubJobSite>(client =>
                    client.BaseAddress = new Uri(address.AbsoluteUri.TrimEnd('/') + "/"));
                return services;
            default:
                throw new InvalidOperationException($"Unsupported JobSites:Type: '{options.Type}'.");
        }
    }
}
