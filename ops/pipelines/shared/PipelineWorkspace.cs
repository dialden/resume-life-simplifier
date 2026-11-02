using System.Text.Json;

namespace DialDen.RLS.Pipelines;

public static class PipelineWorkspace
{
    public const string ConfigFile = "ops/pipelines/config.json";

    public static string ResolveRoot()
    {
        var root = Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root))
        {
            throw new InvalidOperationException("Set WORKSPACE_ROOT to the absolute path of the repository root.");
        }
        var configPath = Path.Combine(root, ConfigFile);
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("An expected file is missing.", configPath);
        }
        return root;
    }

    public static PipelineConfig ReadConfig(string root) =>
        JsonSerializer.Deserialize<PipelineConfig>(
            File.ReadAllText(Path.Combine(root, ConfigFile)), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException($"{ConfigFile} is empty.");
}
