namespace DialDen.RLS.Tests.Fixtures;

public class RLSConfiguration
{
    private string? _uiUrl;

    public string WebServerUrl { get; }

    /// <summary>Only available once the UI host has been started.</summary>
    public string UIUrl => _uiUrl ?? throw new InvalidOperationException("The UI host has not been started.");

    public RLSConfiguration(string webServerUrl)
    {
        WebServerUrl = webServerUrl;
    }

    internal void SetUIUrl(string uiUrl) => _uiUrl = uiUrl;
}
