namespace DialDen.RLS.Tests.Fixtures;

public sealed class HostingConfiguration
{
    public HostingMode Mode { get; set; } = HostingMode.InProcess;
    public string APIUrl { get; set; } = "";
    public string UIUrl { get; set; } = "";

    public void Validate()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new InvalidOperationException($"Unsupported hosting mode: {Mode}.");
        }

        if (Mode == HostingMode.External)
        {
            ValidateUrl(APIUrl, nameof(APIUrl));
            ValidateUrl(UIUrl, nameof(UIUrl));
        }
    }

    private static void ValidateUrl(string? value, string name)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host))
        {
            throw new InvalidOperationException(
                $"Hosting:{name} must be an absolute HTTP or HTTPS URL when hosting mode is External.");
        }
    }
}
