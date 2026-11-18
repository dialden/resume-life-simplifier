using System.Collections.Concurrent;
using System.Diagnostics;
using DialDen.RLS.Tests.Core.Clients.API;
using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Drivers.JobSites;
using DialDen.RLS.Tests.Core.Drivers.ResumeAnalysis;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.API;
using DialDen.RLS.Tests.Core.Drivers.RLS.UI;
using DialDen.RLS.Tests.Core.Dsl;
using DialDen.RLS.Tests.Core.Dsl.External.JobSites;
using DialDen.RLS.Tests.Core.Dsl.External.ResumeAnalysis;
using DialDen.RLS.Tests.Core.Dsl.UseCases;
using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;
using Npgsql;
using RestEase;
using WireMock.Client;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>
/// Assembly-fixture entry point that owns shared resources and composes isolated test scopes.
/// A scenario acquires its environment only when its final assertion chain is awaited.
/// </summary>
public sealed class TestHarness : IAsyncLifetime, IDatabaseCleanup
{
    private readonly string _templateName = $"rls_template_{Guid.NewGuid():N}";
    private readonly ConcurrentDictionary<string, string> _databases = new();
    private readonly Lazy<Task<IBrowser>> _browser;
    private readonly IConfiguration _testsConfiguration = new ConfigurationBuilder()
        .AddEnvironmentVariables("RLS_TEST_")
        .AddCommandLine(Environment.GetCommandLineArgs())
        .Build();
    private readonly IConfiguration _workerConfiguration = new ConfigurationBuilder()
        .AddEnvironmentVariables("RLS_WORKER_")
        .AddEnvironmentVariables("STUBBED_EXT_SYSTEMS_RLS_WORKER_")
        .Build();
    private readonly IConfiguration _realWorkerConfiguration = new ConfigurationBuilder()
        .AddEnvironmentVariables("RLS_WORKER_")
        .AddEnvironmentVariables("REAL_EXT_SYSTEMS_RLS_WORKER_")
        .Build();
    private IPlaywright? _playwright;
    private string? _connectionString;
    private string? _adminConnectionString;
    private HttpClient? _jobSitesClient;
    private HttpClient? _resumeAnalysisClient;
    private bool _templateCreationAttempted;
    private bool _disposed;

    public TestHarness()
    {
        _browser = new(CreateBrowserAsync);
    }

    private Uri JobSitesUrl => StubUrl("JobSites:WireMockUrl", "http://wiremock-job-site/");
    private Uri ResumeAnalysisUrl => StubUrl("ResumeAnalysis:WireMockUrl", "http://wiremock-ollama/");
    private IWireMockAdminApi JobSitesAdmin => new RestClient(_jobSitesClient
        ?? throw new InvalidOperationException("Harness resources have not been initialized.")).For<IWireMockAdminApi>();
    private IWireMockAdminApi ResumeAnalysisAdmin => new RestClient(_resumeAnalysisClient
        ?? throw new InvalidOperationException("Harness resources have not been initialized.")).For<IWireMockAdminApi>();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        try
        {
            await InitializeResourcesAsync();
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, DisposeResourcesAsync);
            throw;
        }
    }

    public TestScenarioSelection For(ExternalSystems externalSystems)
    {
        if (!Enum.IsDefined(externalSystems))
            throw new ArgumentOutOfRangeException(nameof(externalSystems));
        return new(channel => CreateScopeAsync(externalSystems, channel));
    }

    private async ValueTask<TestScope> CreateScopeAsync(ExternalSystems externalSystems, Channel channel)
    {
        var environment = await StartEnvironmentAsync(externalSystems, channel);
        ServiceProvider? services = null;
        AsyncServiceScope? interactionScope = null;
        try
        {
            services = CreateServices(environment, channel);
            interactionScope = services.CreateAsyncScope();
            var provider = interactionScope.Value.ServiceProvider;
            var interaction = new UseCaseDsl(
                provider.GetRequiredService<RLSDsl>(),
                provider.GetRequiredService<ExternalSystemsDsl>());
            var uiClient = channel == Channel.UI ? provider.GetRequiredService<UIClient>() : null;
            return new(environment, interaction, services, interactionScope.Value, uiClient);
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error,
                () => interactionScope?.DisposeAsync() ?? ValueTask.CompletedTask,
                () => services?.DisposeAsync() ?? ValueTask.CompletedTask,
                environment.DisposeAsync);
            throw;
        }
    }

    ValueTask IAsyncDisposable.DisposeAsync() => DisposeResourcesAsync();

    private async ValueTask<TestEnvironment> StartEnvironmentAsync(ExternalSystems externalSystems, Channel channel)
    {
        var database = await CreateDatabaseAsync();
        TestEnvironment? environment = null;
        try
        {
            environment = CreateEnvironment(database, externalSystems, channel);
            environment.Start();
            return environment;
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, () => environment is not null
                ? environment.DisposeAsync()
                : DropDatabaseAsync(database.Name));
            throw;
        }
    }

    private TestEnvironment CreateEnvironment(
        (string Name, string ConnectionString) database, ExternalSystems externalSystems, Channel channel)
    {
        var workerConfiguration = externalSystems switch
        {
            ExternalSystems.Stubbed => _workerConfiguration,
            ExternalSystems.Real => _realWorkerConfiguration,
            _ => throw new ArgumentOutOfRangeException(nameof(externalSystems), externalSystems, null)
        };
        var stubbed = externalSystems == ExternalSystems.Stubbed;
        return new(database.Name, database.ConnectionString, externalSystems, channel,
            workerConfiguration,
            stubbed ? (JobSitesUrl, JobSitesAdmin) : null,
            stubbed ? (ResumeAnalysisUrl, ResumeAnalysisAdmin) : null,
            this);
    }

    private ServiceProvider CreateServices(TestEnvironment environment, Channel channel)
    {
        var services = new ServiceCollection();
        services.AddSingleton(environment.Configuration);
        services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = new Uri(environment.Configuration.WebServerUrl)
        });
        services.AddScoped<APIClient>();
        if (channel == Channel.UI)
        {
            services.AddScoped(_ => new UIClient(environment.Configuration, GetBrowserAsync));
            services.AddScoped<IRLSDriver, UIRLSDriver>();
        }
        else
        {
            services.AddScoped<IRLSDriver, APIRLSDriver>();
        }
        services.AddScoped<UseCaseContext>();
        services.AddScoped<GoToRLS>();
        services.AddScoped<UseCaseFactory<IRLSDriver>>();
        services.AddScoped<RLSDsl>();
        services.AddScoped<ExternalSystemsDsl>();
        services.AddScoped<JobSitesDsl>();
        services.AddScoped<ResumeAnalysisDsl>();
        if (environment.JobSiteMappings is { } jobSites && environment.ResumeAnalysisMappings is { } resumeAnalysis)
        {
            services.AddScoped<IJobSitesDriver>(_ => new WireMockJobSitesDriver(
                JobSitesAdmin, environment.JobSiteIds, jobSites));
            services.AddScoped<IResumeAnalysisDriver>(_ => new WireMockResumeAnalysisDriver(
                _testsConfiguration["ResumeAnalysis:CallbackHost"] ?? "localhost", resumeAnalysis));
        }
        else
        {
            services.AddScoped<IJobSitesDriver, RealJobSitesDriver>();
            services.AddScoped<IResumeAnalysisDriver, RealResumeAnalysisDriver>();
        }
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private async ValueTask InitializeResourcesAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (TestConfiguration.From(_testsConfiguration).Hosting.Mode != HostingMode.InProcess)
            throw new NotSupportedException("The harness currently supports in-process hosting only.");

        var settings = CreateDatabaseSettings();
        _connectionString = settings.ConnectionString;
        _adminConnectionString = new NpgsqlConnectionStringBuilder(settings.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        }.ConnectionString;
        InitializeWireMockClients();
        await CreateDatabaseTemplateAsync(settings);
    }

    private NpgsqlConnectionStringBuilder CreateDatabaseSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables("RLS_API_")
            .AddCommandLine(Environment.GetCommandLineArgs())
            .AddEnvironmentVariables("STUBBED_EXT_SYSTEMS_RLS_API_")
            .Build();
        var settings = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("RLS")
            ?? throw new InvalidOperationException("The stubbed API test database connection is required."));
        var testDatabase = Environment.GetEnvironmentVariable("RLS_TEST_DATABASE");
        var developmentDatabase = Environment.GetEnvironmentVariable("RLS_DEV_DATABASE");
        if (string.IsNullOrWhiteSpace(testDatabase) || string.IsNullOrWhiteSpace(developmentDatabase)
            || !string.Equals(settings.Database, testDatabase, StringComparison.Ordinal)
            || string.Equals(settings.Database, developmentDatabase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The harness connection must target RLS_TEST_DATABASE, not RLS_DEV_DATABASE.");
        }
        return settings;
    }

    private void InitializeWireMockClients()
    {
        _jobSitesClient = new HttpClient { BaseAddress = JobSitesUrl };
        _resumeAnalysisClient = new HttpClient { BaseAddress = ResumeAnalysisUrl };
    }

    private async ValueTask CreateDatabaseTemplateAsync(NpgsqlConnectionStringBuilder settings)
    {
        _templateCreationAttempted = true;
        await ExecuteAsync($"CREATE DATABASE {Quote(_templateName)} TEMPLATE template0");
        await MigrateAsync(settings);
        await ExecuteAsync($"ALTER DATABASE {Quote(_templateName)} ALLOW_CONNECTIONS false");
    }

    private async ValueTask<(string Name, string ConnectionString)> CreateDatabaseAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var name = $"rls_scope_{Guid.NewGuid():N}";
        var settings = new NpgsqlConnectionStringBuilder(_connectionString
            ?? throw new InvalidOperationException("Harness resources have not been initialized."))
        {
            Database = name
        };
        _databases[name] = settings.ConnectionString;
        try
        {
            await ExecuteAsync($"CREATE DATABASE {Quote(name)} TEMPLATE {Quote(_templateName)}");
            return (name, settings.ConnectionString);
        }
        catch (Exception error)
        {
            await TestResourceCleanup.RunAsync(error, () => DropDatabaseAsync(name));
            throw;
        }
    }

    ValueTask IDatabaseCleanup.DropDatabaseAsync(string databaseName) => DropDatabaseAsync(databaseName);

    private async ValueTask DropDatabaseAsync(string name)
    {
        if (!_databases.TryGetValue(name, out var connectionString))
            throw new InvalidOperationException("Refusing to drop a database not owned by this harness.");
        using (var connection = new NpgsqlConnection(connectionString))
            NpgsqlConnection.ClearPool(connection);
        try
        {
            await ExecuteAsync($"DROP DATABASE IF EXISTS {Quote(name)}");
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.ObjectInUse)
        {
            string sessions;
            try
            {
                sessions = await DescribeDatabaseSessionsAsync(name);
            }
            catch (NpgsqlException diagnosticError)
            {
                throw new AggregateException($"Could not inspect sessions for scenario database '{name}'.",
                    error, diagnosticError);
            }
            throw new InvalidOperationException($"Could not drop scenario database '{name}'. Sessions: {sessions}.", error);
        }
        _databases.TryRemove(name, out _);
    }

    private async Task<string> DescribeDatabaseSessionsAsync(string name)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COALESCE(string_agg(
                format('pid=%s type=%s state=%s wait=%s/%s',
                    pid, backend_type, state, wait_event_type, wait_event), '; ' ORDER BY pid),
                'none visible at diagnostic time')
            FROM pg_stat_activity
            WHERE datname = @name
            """, connection);
        command.Parameters.AddWithValue("name", name);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async ValueTask DisposeResourcesAsync()
    {
        if (_disposed) return;
        _disposed = true;
        var cleanup = _databases.Keys.Select<string, Func<ValueTask>>(name => () => DropDatabaseAsync(name)).ToList();
        cleanup.Add(async () =>
        {
            if (!_templateCreationAttempted) return;
            await ExecuteAsync($"DROP DATABASE IF EXISTS {Quote(_templateName)}");
            _templateCreationAttempted = false;
        });
        cleanup.Add(() => { _jobSitesClient?.Dispose(); return ValueTask.CompletedTask; });
        cleanup.Add(() => { _resumeAnalysisClient?.Dispose(); return ValueTask.CompletedTask; });
        cleanup.Add(async () =>
        {
            if (_browser.IsValueCreated)
                await (await _browser.Value).CloseAsync();
        });
        cleanup.Add(() => { _playwright?.Dispose(); return ValueTask.CompletedTask; });
        await TestResourceCleanup.RunAsync(cleanup.ToArray());
    }

    private Task<IBrowser> GetBrowserAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _browser.Value;
    }

    private async Task<IBrowser> CreateBrowserAsync()
    {
        _playwright = await Playwright.CreateAsync();
        return await _playwright[PlaywrightSettingsProvider.BrowserName].LaunchAsync();
    }

    private async Task MigrateAsync(NpgsqlConnectionStringBuilder settings)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "ops", "database", "flyway.toml")))
            directory = directory.Parent;
        if (directory is null)
            throw new DirectoryNotFoundException("Could not locate ops/database/flyway.toml.");
        if (string.IsNullOrWhiteSpace(settings.Host) || settings.Host.StartsWith("/", StringComparison.Ordinal))
            throw new NotSupportedException("The harness Flyway migration requires a TCP PostgreSQL host.");

        var address = new UriBuilder("postgresql", settings.Host, settings.Port, _templateName).Uri;
        var startInfo = new ProcessStartInfo("flyway")
        {
            WorkingDirectory = Path.Combine(directory.FullName, "ops", "database"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-configFiles=flyway.toml");
        startInfo.ArgumentList.Add($"-url=jdbc:{address}");
        startInfo.ArgumentList.Add("migrate");
        startInfo.Environment["FLYWAY_USER"] = settings.Username;
        startInfo.Environment["FLYWAY_PASSWORD"] = settings.Password;
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Flyway.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var diagnostics = await output + Environment.NewLine + await error;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Flyway failed with exit code {process.ExitCode}.{Environment.NewLine}{diagnostics}");
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString
            ?? throw new InvalidOperationException("Harness resources have not been initialized."));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private Uri StubUrl(string key, string defaultUrl)
    {
        if (!Uri.TryCreate(_testsConfiguration[key] ?? defaultUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException($"{key} must be an absolute HTTP(S) base URL without a query or fragment.");
        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    }

    private static string Quote(string name)
    {
        using var builder = new NpgsqlCommandBuilder();
        return builder.QuoteIdentifier(name);
    }
}
