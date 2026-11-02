using System.Text.RegularExpressions;

namespace DialDen.RLS.Pipelines;

public sealed record GoogleCloudConfig(string ProjectId, string ProjectNumber, string Region,
    RemoteRegistryConfig? Registry = null, DatabaseConfig? Database = null, JobillicoConfig? Jobillico = null)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectId) ||
            !Regex.IsMatch(ProjectNumber ?? "", @"\A[0-9]+\z") ||
            string.IsNullOrWhiteSpace(Region))
        {
            throw new InvalidOperationException(
                $"The googleCloud section of {PipelineWorkspace.ConfigFile} needs a projectId, " +
                "a numeric projectNumber and a region.");
        }
    }
}
