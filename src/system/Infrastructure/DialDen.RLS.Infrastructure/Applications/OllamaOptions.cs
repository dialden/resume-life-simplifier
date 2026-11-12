namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public Uri? Endpoint { get; set; }
    public string Model { get; set; } = "qwen2.5vl:7b";
    public int NumCtx { get; set; } = 2048;
    public string? DumpDirectory { get; set; }
    public string? IdentityTokenAudience { get; set; }
    public Uri IdentityMetadataEndpoint { get; set; } =
        new("http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/identity");
    public bool AllowInsecureAuthenticatedEndpoint { get; set; }
}
