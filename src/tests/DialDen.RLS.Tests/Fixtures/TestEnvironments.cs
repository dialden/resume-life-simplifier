using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Returns the test environment for the requested external system, creating each one lazily and once.</summary>
public class TestEnvironments : IAsyncLifetime
{
    private readonly Lazy<TestEnvironment> _real = new(() => new RealExternalSystemsTestEnvironment());
    private readonly Lazy<TestEnvironment> _stubbed = new(() => new StubbedExternalSystemsTestEnvironment());

    public async Task InitializeAsync()
    {
        var testDatabase = Environment.GetEnvironmentVariable("RLS_TEST_DATABASE")
            ?? throw new InvalidOperationException("RLS_TEST_DATABASE is required for test database cleanup.");
        var developmentDatabase = Environment.GetEnvironmentVariable("RLS_DEV_DATABASE")
            ?? throw new InvalidOperationException("RLS_DEV_DATABASE is required to protect the development database.");

        var connections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prefix in new[] { "REAL_EXT_SYSTEMS_", "STUBBED_EXT_SYSTEMS_" })
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables("RLS_API_")
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddEnvironmentVariables(prefix + "RLS_API_")
                .Build();
            var connectionString = configuration.GetConnectionString("RLS")
                ?? throw new InvalidOperationException($"The {prefix} test database connection is required.");
            var settings = new NpgsqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(testDatabase) ||
                !string.Equals(settings.Database, testDatabase, StringComparison.Ordinal) ||
                string.Equals(settings.Database, developmentDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Refusing test cleanup: the connection must target RLS_TEST_DATABASE, not RLS_DEV_DATABASE.");
            }
            connections.Add(settings.ConnectionString);
        }

        foreach (var connectionString in connections)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                TRUNCATE TABLE applications, job_site_publications, postings,
                    messaging."OutboxMessage", messaging."OutboxState", messaging."InboxState",
                    transport.message_delivery, transport.message
                RESTART IDENTITY;
                """, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    //TODO: Add support to control parallel execution of test environments
    public TestEnvironment For(ExternalSystems externalSystem) => externalSystem switch
    {
        ExternalSystems.Real => _real.Value,
        ExternalSystems.Stubbed => _stubbed.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(externalSystem))
    };

    public async Task DisposeAsync()
    {
        if (_real.IsValueCreated) await _real.Value.DisposeAsync();
        if (_stubbed.IsValueCreated) await _stubbed.Value.DisposeAsync();
    }
}
