using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.JobSites.Jobillico.DependencyInjection;

public static class JobillicoServiceCollectionExtensions
{
    public static IServiceCollection AddJobillicoDependencies(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        ArgumentNullException.ThrowIfNull(configuration);
        var options = configuration.GetSection("Jobillico").Get<JobillicoOptions>()
            ?? new JobillicoOptions();
        if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException("Jobillico:Username and Jobillico:Password are required.");
        }
        if (!long.TryParse(options.RecruiterId, out var recruiterId) || recruiterId <= 0
            || !long.TryParse(options.LocationId, out var locationId) || locationId <= 0)
        {
            throw new InvalidOperationException("Jobillico:RecruiterId and Jobillico:LocationId must be positive integers.");
        }
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException("Jobillico:Endpoint must be an absolute HTTP(S) URL without credentials, query, or fragment.");
        }
        if (endpoint.Host.Equals("www.jobillico.com", StringComparison.OrdinalIgnoreCase)
            && endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("The Jobillico endpoint requires HTTPS.");
        }
        services.AddSingleton(options);
        services.AddHttpClient("Jobillico", client => client.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        return services;
    }
}
