#!/usr/bin/env -S dotnet run

#:property PublishAot=false
#:project ../../shared/DialDen.RLS.Pipelines.csproj

using System.Text.RegularExpressions;
using System.Text.Json;

using DialDen.RLS.Pipelines;
using static DialDen.RLS.Pipelines.PipelineLog;

var (version, environment) = ReleaseStage.ParseArguments(args);
var stage = new ReleaseStage(version, environment);

await stage.ExportMigrations();
await stage.VerifyGoogleCloudAuthentication();
await stage.VerifyWorkerDeploymentPrerequisites();
await stage.EnableRegistryApis();
await stage.EnsureRegistryTokenSecret();
await stage.GrantRegistryTokenAccess();
await stage.EnsureRemoteRepository();
await stage.VerifyImages();
await stage.EnsureDatabaseInstance();
await stage.EnsureApplicationDatabase();
await stage.EnsureDatabasePasswords();
await stage.EnsureDatabaseMigrator();
await stage.VerifyMigratorDatabaseLogin();
await stage.EnsureApplicationDatabaseLogin();
await stage.MigrateDatabase();
await stage.EnsureApplicationDatabasePermissions();
await stage.EnsureRuntimeDatabaseConnection();
await stage.EnsureRuntimeIdentities();
await stage.EnsureWorkerJobSiteCredentials();
await stage.DeployWorker();
await stage.DeployApi();
await stage.DeployUi();

stage.ReportCompletion();

public sealed class ReleaseStage
{
    private readonly string version;
    private readonly string environment;
    private readonly GoogleCloudConfig googleCloud;
    private readonly string artifacts;
    private readonly GoogleCloudClient googleCloudClient;
    private readonly ImagesConfig images;
    private readonly CloudSqlClient cloudSqlClient;
    private readonly CloudSqlProxyClient cloudSqlProxyClient;
    private readonly DatabaseConfig database;
    private readonly ReleaseMigrations releaseMigrations;
    private readonly GoogleCloudIamClient googleCloudIamClient;
    private readonly CloudRunClient cloudRunClient;
    private ImagesConfig? resolvedImages;
    private string? workerUsernameSecret;
    private string? workerPasswordSecret;
    private string? migrationsDirectory;
    private string? apiUrl;

    public ReleaseStage(string version, string environment)
    {
        this.version = version;
        this.environment = environment;
        var root = PipelineWorkspace.ResolveRoot();
        var config = PipelineWorkspace.ReadConfig(root);
        images = config.Images;
        googleCloud = config.GoogleCloud
            ?? throw new InvalidOperationException($"{PipelineWorkspace.ConfigFile} has no googleCloud section.");
        googleCloud.Validate();
        database = googleCloud.Database
            ?? throw new InvalidOperationException("The googleCloud.database configuration is missing.");
        database.Validate();
        var jobSite = googleCloud.Jobillico
            ?? throw new InvalidOperationException("The googleCloud.jobillico configuration is missing.");
        jobSite.Validate();

        artifacts = Path.Combine(root, ".artifacts", "release", environment,
            $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(artifacts);
        var processRunner = new ProcessRunner(root, artifacts);
        releaseMigrations = new ReleaseMigrations(processRunner);
        googleCloudClient = new GoogleCloudClient(processRunner, googleCloud);
        googleCloudIamClient = new GoogleCloudIamClient(processRunner, googleCloud);
        cloudRunClient = new CloudRunClient(processRunner, googleCloud);
        cloudSqlClient = new CloudSqlClient(processRunner, googleCloud);
        cloudSqlProxyClient = new CloudSqlProxyClient(processRunner, googleCloudClient);
        Write($"Starting release of version {version} to environment '{environment}' " +
            $"in {googleCloud.ProjectId} ({googleCloud.Region}). Artifacts: {artifacts}");
    }

    public Task VerifyGoogleCloudAuthentication() => googleCloudClient.VerifyAuthentication();

    public async Task ExportMigrations() =>
        migrationsDirectory = await releaseMigrations.Export(version, artifacts);

    public Task EnableRegistryApis() =>
        googleCloudClient.EnableApis("artifactregistry.googleapis.com", "secretmanager.googleapis.com");

    private RemoteRegistryConfig Registry => googleCloud.Registry
        ?? throw new InvalidOperationException("The googleCloud.registry configuration is missing.");

    public Task EnsureRegistryTokenSecret() => googleCloudClient.EnsureSecret(Registry.TokenSecret);

    public Task EnsureRemoteRepository() => googleCloudClient.EnsureRemoteRepository(Registry);

    public async Task VerifyImages()
    {
        var resolved = new ImagesConfig(
            await googleCloudClient.ResolveRemoteImage(images.Api, version, Registry),
            await googleCloudClient.ResolveRemoteImage(images.Ui, version, Registry),
            await googleCloudClient.ResolveRemoteImage(images.Worker, version, Registry));
        resolvedImages = resolved;
        await File.WriteAllTextAsync(Path.Combine(artifacts, "images.json"),
            JsonSerializer.Serialize(resolved, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
    }

    public async Task GrantRegistryTokenAccess()
    {
        await googleCloudClient.EnsureServiceIdentity("artifactregistry.googleapis.com");
        await googleCloudClient.GrantSecretAccess(Registry.TokenSecret,
            $"service-{googleCloud.ProjectNumber}@gcp-sa-artifactregistry.iam.gserviceaccount.com");
    }

    public async Task EnsureDatabaseInstance()
    {
        await googleCloudClient.EnableApis("sqladmin.googleapis.com");
        var connectionName = await cloudSqlClient.EnsureInstance($"rls-{environment}", database);
        await File.WriteAllTextAsync(Path.Combine(artifacts, "database.json"),
            JsonSerializer.Serialize(new { connectionName }, new JsonSerializerOptions { WriteIndented = true }));
    }

    public Task EnsureApplicationDatabase() => cloudSqlClient.EnsureDatabase($"rls-{environment}", "rls");

    public async Task EnsureDatabasePasswords()
    {
        await googleCloudClient.EnsureGeneratedPassword($"rls-{environment}-db-migrator-password");
        await googleCloudClient.EnsureGeneratedPassword($"rls-{environment}-db-app-password");
    }

    public Task EnsureDatabaseMigrator() => cloudSqlClient.EnsureMigrator(
        $"rls-{environment}", googleCloudClient, $"rls-{environment}-db-migrator-password");

    public async Task VerifyMigratorDatabaseLogin()
    {
        var instanceName = $"rls-{environment}";
        var connectionName = await cloudSqlClient.EnsureInstance(instanceName, database);
        await cloudSqlProxyClient.VerifyDatabaseLogin(connectionName, "rls", "rls_migrator",
            $"rls-{environment}-db-migrator-password");
    }

    public async Task EnsureApplicationDatabaseLogin()
    {
        var instanceName = $"rls-{environment}";
        var connectionName = await cloudSqlClient.EnsureInstance(instanceName, database);
        await cloudSqlProxyClient.EnsureApplicationDatabaseLogin(connectionName, "rls",
            $"rls-{environment}-db-migrator-password", $"rls-{environment}-db-app-password");
    }

    public async Task MigrateDatabase()
    {
        var exportedMigrations = migrationsDirectory
            ?? throw new InvalidOperationException("Export release migrations before applying them.");
        var connectionName = await cloudSqlClient.EnsureInstance($"rls-{environment}", database);
        await cloudSqlProxyClient.MigrateDatabase(connectionName, "rls",
            $"rls-{environment}-db-migrator-password", exportedMigrations);
    }

    public async Task EnsureApplicationDatabasePermissions()
    {
        var connectionName = await cloudSqlClient.EnsureInstance($"rls-{environment}", database);
        await cloudSqlProxyClient.EnsureApplicationDatabasePermissions(connectionName, "rls",
            $"rls-{environment}-db-migrator-password", $"rls-{environment}-db-app-password");
    }

    public async Task EnsureRuntimeDatabaseConnection()
    {
        var connectionName = await cloudSqlClient.EnsureInstance($"rls-{environment}", database);
        await cloudSqlClient.EnsureRuntimeConnectionSecret(connectionName, "rls", googleCloudClient,
            $"rls-{environment}-db-app-password", $"rls-{environment}-db-connection");
    }

    public async Task EnsureRuntimeIdentities()
    {
        await googleCloudClient.EnableApis("iam.googleapis.com");
        var api = await googleCloudIamClient.EnsureServiceAccount($"rls-{environment}-api");
        var worker = await googleCloudIamClient.EnsureServiceAccount($"rls-{environment}-wrk");
        var ui = await googleCloudIamClient.EnsureServiceAccount($"rls-{environment}-ui");
        foreach (var account in new[] { api, worker })
        {
            await googleCloudIamClient.GrantCloudSqlClient(account);
            await googleCloudClient.GrantSecretAccess($"rls-{environment}-db-connection", account);
        }
        await File.WriteAllTextAsync(Path.Combine(artifacts, "runtime-identities.json"),
            JsonSerializer.Serialize(new { api, worker, ui },
                new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task EnsureWorkerJobSiteCredentials()
    {
        var usernameSecret = $"rls-{environment}-jobillico-username";
        var passwordSecret = $"rls-{environment}-jobillico-password";
        var usernameVersion = await googleCloudClient.ResolveSecretVersion(usernameSecret);
        var passwordVersion = await googleCloudClient.ResolveSecretVersion(passwordSecret);
        workerUsernameSecret = $"{usernameSecret}:{usernameVersion}";
        workerPasswordSecret = $"{passwordSecret}:{passwordVersion}";
        var worker = $"rls-{environment}-wrk@{googleCloud.ProjectId}.iam.gserviceaccount.com";
        await googleCloudClient.GrantSecretAccess(usernameSecret, worker);
        await googleCloudClient.GrantSecretAccess(passwordSecret, worker);
        await File.WriteAllTextAsync(Path.Combine(artifacts, "worker-job-site-secrets.json"),
            JsonSerializer.Serialize(new
            {
                username = $"{usernameSecret}:{usernameVersion}",
                password = $"{passwordSecret}:{passwordVersion}"
            }, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task VerifyWorkerDeploymentPrerequisites()
    {
        await googleCloudClient.EnableApis("run.googleapis.com", "cloudresourcemanager.googleapis.com");
        await cloudRunClient.RequireWorkerStopped($"rls-{environment}-worker");
    }

    public async Task DeployWorker()
    {
        var image = resolvedImages?.Worker
            ?? throw new InvalidOperationException("Resolve the Worker image before deploying.");
        var usernameSecret = workerUsernameSecret
            ?? throw new InvalidOperationException("Resolve the Jobillico username secret before deploying.");
        var passwordSecret = workerPasswordSecret
            ?? throw new InvalidOperationException("Resolve the Jobillico password secret before deploying.");
        var connectionSecret = $"rls-{environment}-db-connection";
        var connectionVersion = await googleCloudClient.ResolveSecretVersion(connectionSecret);
        var connectionName = await cloudSqlClient.EnsureInstance($"rls-{environment}", database);
        await cloudRunClient.DeployWorker($"rls-{environment}-worker", image, connectionName,
            $"rls-{environment}-wrk@{googleCloud.ProjectId}.iam.gserviceaccount.com",
            googleCloud.Jobillico ?? throw new InvalidOperationException("Jobillico configuration is missing."),
            $"{connectionSecret}:{connectionVersion}", usernameSecret, passwordSecret, artifacts);
    }

    public async Task DeployApi()
    {
        var image = resolvedImages?.Api
            ?? throw new InvalidOperationException("Resolve the API image before deploying.");
        var connectionSecret = $"rls-{environment}-db-connection";
        var connectionVersion = await googleCloudClient.ResolveSecretVersion(connectionSecret);
        var connectionName = await cloudSqlClient.EnsureInstance($"rls-{environment}", database);
        apiUrl = await cloudRunClient.DeployApi($"rls-{environment}-api", image, connectionName,
            $"rls-{environment}-api@{googleCloud.ProjectId}.iam.gserviceaccount.com",
            $"{connectionSecret}:{connectionVersion}", artifacts);
    }

    public async Task DeployUi()
    {
        var image = resolvedImages?.Ui
            ?? throw new InvalidOperationException("Resolve the UI image before deploying.");
        var deployedApiUrl = apiUrl
            ?? throw new InvalidOperationException("Deploy the API and discover its URL before deploying the UI.");
        await cloudRunClient.DeployUi($"rls-{environment}-ui", image,
            $"rls-{environment}-ui@{googleCloud.ProjectId}.iam.gserviceaccount.com",
            $"rls-{environment}-api", deployedApiUrl, artifacts);
    }

    public void ReportCompletion() =>
        Write($"Release stage completed. Version {version} to environment '{environment}'.");

    public static (string Version, string Environment) ParseArguments(string[] arguments)
    {
        string? version = null;
        string? environment = null;
        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--version" when version is null && index + 1 < arguments.Length:
                    version = arguments[++index];
                    break;
                case "--target-environment" when environment is null && index + 1 < arguments.Length:
                    environment = arguments[++index];
                    break;
                default:
                    throw new ArgumentException($"Unknown, duplicate, or incomplete argument: {arguments[index]}");
            }
        }
        version = ImageVersion.Validate(version);
        // Environment names become part of Google Cloud resource names such as rls-{environment}-api.
        // The option is not called --environment because `dotnet run`, used by the shebang, consumes that name.
        if (environment is null || !Regex.IsMatch(environment, @"\A[a-z](?:[a-z0-9-]{0,18}[a-z0-9])?\z"))
        {
            throw new ArgumentException(
                "Supply --target-environment as 1-20 lowercase letters, digits or hyphens, " +
                "starting with a letter and not ending with a hyphen.");
        }
        return (version, environment);
    }

}
