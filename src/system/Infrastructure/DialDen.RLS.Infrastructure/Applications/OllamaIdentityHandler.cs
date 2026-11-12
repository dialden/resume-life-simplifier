using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class OllamaIdentityHandler(IHttpClientFactory clients, IOptions<OllamaOptions> options)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var audience = options.Value.IdentityTokenAudience;
        if (!string.IsNullOrWhiteSpace(audience))
        {
            var endpoint = options.Value.IdentityMetadataEndpoint;
            var builder = new UriBuilder(endpoint)
            {
                Query = $"audience={Uri.EscapeDataString(audience)}"
            };
            using var metadataRequest = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
            metadataRequest.Headers.Add("Metadata-Flavor", "Google");
            using var response = await clients.CreateClient("OllamaIdentityMetadata")
                .SendAsync(metadataRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (!response.Headers.TryGetValues("Metadata-Flavor", out var flavors) ||
                !flavors.Contains("Google", StringComparer.Ordinal))
                throw new InvalidDataException("The identity metadata server did not identify itself as Google.");
            var token = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            if (string.IsNullOrWhiteSpace(token) || token.Any(char.IsWhiteSpace))
                throw new InvalidDataException("The identity metadata server returned an invalid token.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
