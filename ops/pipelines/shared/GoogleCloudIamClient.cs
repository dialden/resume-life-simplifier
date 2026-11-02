using System.Text.Json;
using System.Text.RegularExpressions;

namespace DialDen.RLS.Pipelines;

public sealed class GoogleCloudIamClient(ProcessRunner processRunner, GoogleCloudConfig config)
{
    public async Task<string> EnsureServiceAccount(string accountId)
    {
        if (!Regex.IsMatch(accountId, @"\A[a-z][a-z0-9-]{4,28}[a-z0-9]\z"))
        {
            throw new ArgumentException(
                "Service account IDs must contain 6-30 lowercase letters, digits or hyphens, " +
                "starting with a letter and not ending with a hyphen.", nameof(accountId));
        }

        var email = $"{accountId}@{config.ProjectId}.iam.gserviceaccount.com";
        using var accounts = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["iam", "service-accounts", "list", $"--project={config.ProjectId}", "--format=json", "--quiet"],
            captureOutput: true));
        if (!accounts.RootElement.EnumerateArray().Any(account =>
            account.GetProperty("email").GetString() == email))
        {
            await processRunner.Run("gcloud",
                ["iam", "service-accounts", "create", accountId, $"--project={config.ProjectId}", "--quiet"]);
        }

        using var account = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["iam", "service-accounts", "describe", email, $"--project={config.ProjectId}", "--format=json", "--quiet"],
            captureOutput: true));
        if (account.RootElement.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean())
        {
            throw new InvalidOperationException($"Runtime service account '{email}' is disabled; it was not re-enabled.");
        }
        PipelineLog.Write($"Runtime service account verified: {email}.");
        return email;
    }

    public Task<string> GrantCloudSqlClient(string serviceAccount) =>
        processRunner.Run("gcloud",
            ["projects", "add-iam-policy-binding", config.ProjectId,
                $"--member=serviceAccount:{serviceAccount}", "--role=roles/cloudsql.client",
                "--condition=None", "--quiet"]);
}
