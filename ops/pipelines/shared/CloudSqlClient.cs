using System.Globalization;
using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DialDen.RLS.Pipelines;

public sealed class CloudSqlClient(ProcessRunner processRunner, GoogleCloudConfig config)
{
    public async Task EnsureRuntimeConnectionSecret(
        string connectionName, string database, GoogleCloudClient secrets,
        string passwordSecret, string connectionSecret)
    {
        var password = await secrets.ReadSecret(passwordSecret);
        var connectionString = new DbConnectionStringBuilder
        {
            ["Host"] = $"/cloudsql/{connectionName}",
            ["Port"] = 5432,
            ["Database"] = database,
            ["Username"] = "rls_app",
            ["Password"] = password,
            ["SSL Mode"] = "Disable",
            ["Pooling"] = true,
            ["Minimum Pool Size"] = 0,
            ["Maximum Pool Size"] = 10
        };
        await secrets.EnsureSecretValue(connectionSecret, connectionString.ConnectionString);
        PipelineLog.Write($"Runtime database connection stored in secret '{connectionSecret}'.");
    }

    public async Task EnsureMigrator(string instanceName, GoogleCloudClient secrets, string passwordSecret)
    {
        const string userName = "rls_migrator";
        using var users = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["sql", "users", "list", $"--instance={instanceName}", $"--project={config.ProjectId}",
                "--format=json", "--quiet"], captureOutput: true));
        if (users.RootElement.EnumerateArray().Any(user => user.GetProperty("name").GetString() == userName))
        {
            PipelineLog.Write($"Migration user '{userName}' already exists; its password and privileges are unchanged.");
            return;
        }

        var password = await secrets.ReadSecret(passwordSecret);
        // The Linux dev container exposes stdin as a file, keeping the password out of argv and disk.
        var flags = JsonSerializer.Serialize(new Dictionary<string, string> { ["--password"] = password });
        await processRunner.Run("gcloud",
            ["sql", "users", "create", userName, $"--instance={instanceName}", $"--project={config.ProjectId}",
                "--type=BUILT_IN", "--flags-file=/dev/stdin", "--quiet"], standardInput: flags);

        using var createdUsers = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["sql", "users", "list", $"--instance={instanceName}", $"--project={config.ProjectId}",
                "--format=json", "--quiet"], captureOutput: true));
        if (!createdUsers.RootElement.EnumerateArray().Any(user => user.GetProperty("name").GetString() == userName))
        {
            throw new InvalidOperationException($"Migration user '{userName}' was not found after creation.");
        }
        PipelineLog.Write($"Created migration user '{userName}'. Database login verification awaits proxy setup.");
    }

    public async Task EnsureDatabase(string instanceName, string databaseName)
    {
        if (!Regex.IsMatch(databaseName, @"\A[a-z][a-z0-9_]{0,62}\z"))
        {
            throw new ArgumentException(
                "Database names must contain 1-63 lowercase letters, digits or underscores, starting with a letter.",
                nameof(databaseName));
        }

        using var databases = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["sql", "databases", "list", $"--instance={instanceName}", $"--project={config.ProjectId}",
                "--format=json", "--quiet"], captureOutput: true));
        if (!databases.RootElement.EnumerateArray().Any(database =>
            database.GetProperty("name").GetString() == databaseName))
        {
            await processRunner.Run("gcloud",
                ["sql", "databases", "create", databaseName, $"--instance={instanceName}",
                    $"--project={config.ProjectId}", "--charset=UTF8", "--quiet"]);
        }

        using var description = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["sql", "databases", "describe", databaseName, $"--instance={instanceName}",
                $"--project={config.ProjectId}", "--format=json", "--quiet"], captureOutput: true));
        if (description.RootElement.GetProperty("charset").GetString() != "UTF8")
        {
            throw new InvalidOperationException(
                $"Database '{databaseName}' on '{instanceName}' must use UTF8 encoding. Existing data was not modified.");
        }
        PipelineLog.Write($"Application database verified: {instanceName}/{databaseName}.");
    }

    public async Task<string> EnsureInstance(string instanceName, DatabaseConfig database)
    {
        database.Validate();
        if (!Regex.IsMatch(instanceName, @"\A[a-z][a-z0-9-]{0,96}[a-z0-9]\z"))
        {
            throw new ArgumentException("Supply a valid Cloud SQL instance name.", nameof(instanceName));
        }

        using var instances = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["sql", "instances", "list", $"--project={config.ProjectId}", "--format=json", "--quiet"],
            captureOutput: true));
        if (!instances.RootElement.EnumerateArray().Any(instance =>
            instance.GetProperty("name").GetString() == instanceName))
        {
            await processRunner.Run("gcloud",
                ["sql", "instances", "create", instanceName, $"--project={config.ProjectId}",
                    $"--region={config.Region}", $"--database-version={database.Version}",
                    $"--edition={database.Edition}", $"--tier={database.Tier}",
                    $"--storage-type={database.StorageType}",
                    $"--storage-size={database.StorageSizeGb.ToString(CultureInfo.InvariantCulture)}",
                    $"--availability-type={database.AvailabilityType.ToLowerInvariant()}",
                    database.StorageAutoIncrease ? "--storage-auto-increase" : "--no-storage-auto-increase",
                    database.BackupsEnabled ? "--backup" : "--no-backup",
                    // gcloud rejects even the negated PITR option alongside --no-backup.
                    .. (database.PointInTimeRecoveryEnabled
                        ? new[] { "--enable-point-in-time-recovery" }
                        : Array.Empty<string>()),
                    database.DeletionProtection ? "--deletion-protection" : "--no-deletion-protection",
                    "--assign-ip", "--ssl-mode=ENCRYPTED_ONLY", "--quiet"]);
        }

        using var description = JsonDocument.Parse(await processRunner.Run("gcloud",
            ["sql", "instances", "describe", instanceName, $"--project={config.ProjectId}",
                "--format=json", "--quiet"], captureOutput: true));
        var instance = description.RootElement;
        var settings = instance.GetProperty("settings");
        var backup = settings.GetProperty("backupConfiguration");
        var network = settings.GetProperty("ipConfiguration");
        if (instance.GetProperty("state").GetString() != "RUNNABLE" ||
            instance.GetProperty("region").GetString() != config.Region ||
            instance.GetProperty("databaseVersion").GetString() != database.Version ||
            settings.GetProperty("edition").GetString() != database.Edition ||
            settings.GetProperty("tier").GetString() != database.Tier ||
            settings.GetProperty("dataDiskType").GetString() != $"PD_{database.StorageType}" ||
            settings.GetProperty("dataDiskSizeGb").GetString() != database.StorageSizeGb.ToString(CultureInfo.InvariantCulture) ||
            settings.GetProperty("availabilityType").GetString() != database.AvailabilityType ||
            IsEnabled(settings, "storageAutoResize") != database.StorageAutoIncrease ||
            IsEnabled(settings, "deletionProtectionEnabled") != database.DeletionProtection ||
            IsEnabled(backup, "enabled") != database.BackupsEnabled ||
            IsEnabled(backup, "pointInTimeRecoveryEnabled") != database.PointInTimeRecoveryEnabled ||
            !IsEnabled(network, "ipv4Enabled") ||
            network.GetProperty("sslMode").GetString() != "ENCRYPTED_ONLY" ||
            (network.TryGetProperty("authorizedNetworks", out var networks) && networks.GetArrayLength() != 0))
        {
            throw new InvalidOperationException(
                $"Cloud SQL instance '{instanceName}' is not runnable or does not match the configured database settings. " +
                "Inspect it before retrying; the release will not resize, replace, or weaken protection on an existing instance.");
        }
        var connectionName = instance.GetProperty("connectionName").GetString()
            ?? throw new InvalidOperationException($"Cloud SQL instance '{instanceName}' has no connection name.");
        PipelineLog.Write($"Cloud SQL instance verified: {connectionName}.");
        return connectionName;
    }

    private static bool IsEnabled(JsonElement settings, string property) =>
        settings.TryGetProperty(property, out var value) && value.GetBoolean();
}
