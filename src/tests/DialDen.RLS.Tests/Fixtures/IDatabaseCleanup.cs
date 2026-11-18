namespace DialDen.RLS.Tests.Fixtures;

public interface IDatabaseCleanup
{
    ValueTask DropDatabaseAsync(string databaseName);
}
