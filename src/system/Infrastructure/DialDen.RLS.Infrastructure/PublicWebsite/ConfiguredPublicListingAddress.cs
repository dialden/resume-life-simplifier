using DialDen.RLS.Postings;
using Microsoft.Extensions.Configuration;

namespace DialDen.RLS.Infrastructure.PublicWebsite;

internal sealed class ConfiguredPublicListingAddress : IPublicListingAddress
{
    private readonly Lazy<Uri> _baseAddress;

    public ConfiguredPublicListingAddress(IConfiguration configuration)
    {
        _baseAddress = new Lazy<Uri>(() => Validate(configuration["PublicWebsite:BaseUrl"]));
    }

    public Uri ForPosting(int postingId)
    {
        if (postingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(postingId), "Posting IDs must be positive.");
        }

        return new Uri(_baseAddress.Value, $"jobs/{postingId.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    private static Uri Validate(string? baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var address)
            || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(address.UserInfo)
            || address.AbsolutePath != "/"
            || !string.IsNullOrEmpty(address.Query)
            || !string.IsNullOrEmpty(address.Fragment))
        {
            throw new InvalidOperationException(
                "PublicWebsite:BaseUrl must be an absolute HTTP(S) origin without a path, query, or fragment.");
        }

        return new Uri(address.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
    }
}
