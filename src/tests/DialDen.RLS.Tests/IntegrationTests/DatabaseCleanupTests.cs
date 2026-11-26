using DialDen.RLS.Tests.Fixtures;
using Npgsql;

namespace DialDen.RLS.Tests.IntegrationTests;

public class DatabaseCleanupTests(TestHarness harness)
{
    [Fact]
    public async Task CleanupDropsOnlyTheOwnedDatabaseEvenWithAnOpenConnection()
    {
        var first = await harness.CreateDatabaseAsync();
        var second = await harness.CreateDatabaseAsync();
        var firstDropped = false;
        await using var firstConnection = new NpgsqlConnection(first.ConnectionString);
        await using var secondConnection = new NpgsqlConnection(second.ConnectionString);
        try
        {
            await firstConnection.OpenAsync(TestContext.Current.CancellationToken);
            await secondConnection.OpenAsync(TestContext.Current.CancellationToken);

            await harness.DropDatabaseAsync(first.Name);
            firstDropped = true;

            var adminSettings = new NpgsqlConnectionStringBuilder(second.ConnectionString) { Database = "postgres" };
            await using var admin = new NpgsqlConnection(adminSettings.ConnectionString);
            await admin.OpenAsync(TestContext.Current.CancellationToken);
            await using var exists = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @name)", admin);
            exists.Parameters.AddWithValue("name", first.Name);
            Assert.Equal(false, await exists.ExecuteScalarAsync(TestContext.Current.CancellationToken));
            await using var query = new NpgsqlCommand("SELECT 1", secondConnection);
            Assert.Equal(1, await query.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            await firstConnection.DisposeAsync();
            await secondConnection.DisposeAsync();
            if (!firstDropped)
                await harness.DropDatabaseAsync(first.Name);
            await harness.DropDatabaseAsync(second.Name);
        }
    }
}
