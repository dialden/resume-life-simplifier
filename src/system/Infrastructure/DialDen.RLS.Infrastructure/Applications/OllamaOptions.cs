namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public Uri? Endpoint { get; set; }
    public string Model { get; set; } = "qwen2.5vl:7b";
    public int NumCtx { get; set; } = 2048;
}
