using System.Collections.Concurrent;
using System.Diagnostics;
using DialDen.RLS.Tests.Extensions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;
using RestEase;
using WireMock.Client;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Owns the database template, shared stub access, and lazy Playwright/browser instances.</summary>
public sealed class TestHarnessResources : IAsyncDisposable
{
    private readonly string _templateName = $"rls_template_{Guid.NewGuid():N}";
    private readonly ConcurrentDictionary<string, string> _databases = new();
    private readonly Lazy<Task<IBrowser>> _browser;
    private IPlaywright? _playwright;
    private string? _connectionString;
    private string? _adminConnectionString;
    private HttpClient? _jobSitesClient;
    private HttpClient? _resumeAnalysisClient;
    private bool _templateCreationAttempted;
    private bool _disposed;

    public TestHarnessResources()
    {
        _browser = new(CreateBrowserAsync);
    }

    public IConfiguration TestsConfiguration { get; } = new ConfigurationBuilder()
        .AddEnvironmentVariables("RLS_TEST_")
        .AddCommandLine(Environment.GetCommandLineArgs())
        .Build();

    public IConfiguration WorkerConfiguration { get; } = new ConfigurationBuilder()
        .AddEnvironmentVariables("RLS_WORKER_")
        .AddEnvironmentVariables("STUBBED_EXT_SYSTEMS_RLS_WORKER_")
        .Build();

    public IConfiguration RealWorkerConfiguration { get; } = new ConfigurationBuilder()
        .AddEnvironmentVariables("RLS_WORKER_")
        .AddEnvironmentVariables("REAL_EXT_SYSTEMS_RLS_WORKER_")
        .Build();

    public Task<IBrowser> GetBrowserAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _browser.Value;
    }

    public Uri JobSitesUrl => StubUrl("JobSites:WireMockUrl", "http://wiremock-job-site/");
    public Uri ResumeAnalysisUrl => StubUrl("ResumeAnalysis:WireMockUrl", "http://wiremock-ollama/");
    public IWireMockAdminApi JobSitesAdmin => new RestClient(_jobSitesClient
        ?? throw new InvalidOperationException("Harness resources have not been initialized.")).For<IWireMockAdminApi>();
    public IWireMockAdminApi ResumeAnalysisAdmin => new RestClient(_resumeAnalysisClient
        ?? throw new InvalidOperationException("Harness resources have not been initialized.")).For<IWireMockAdminApi>();

    public async ValueTask InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (TestConfiguration.From(TestsConfiguration).Hosting.Mode != HostingMode.InProcess)
            throw new NotSupportedException("The harness currently supports in-process hosting only.");

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
        _connectionString = settings.ConnectionString;
        _adminConnectionString = new NpgsqlConnectionStringBuilder(settings.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        }.ConnectionString;
        _jobSitesClient = new HttpClient { BaseAddress = JobSitesUrl };
        _resumeAnalysisClient = new HttpClient { BaseAddress = ResumeAnalysisUrl };

        _templateCreationAttempted = true;
        await ExecuteAsync($"CREATE DATABASE {Quote(_templateName)} TEMPLATE template0");
        await MigrateAsync(settings);
        await ExecuteAsync($"ALTER DATABASE {Quote(_templateName)} ALLOW_CONNECTIONS false");
    }

    public async ValueTask<(string Name, string ConnectionString)> CreateDatabaseAsync()
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

    public async ValueTask DropDatabaseAsync(string name)
    {
        if (!_databases.TryGetValue(name, out var connectionString))
            throw new InvalidOperationException("Refusing to drop a database not owned by this harness.");
        using (var connection = new NpgsqlConnection(connectionString))
            NpgsqlConnection.ClearPool(connection);
        await ExecuteAsync($"DROP DATABASE IF EXISTS {Quote(name)}");
        _databases.TryRemove(name, out _);
    }

    public async ValueTask DisposeAsync()
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
        if (!Uri.TryCreate(TestsConfiguration[key] ?? defaultUrl, UriKind.Absolute, out var uri)
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
