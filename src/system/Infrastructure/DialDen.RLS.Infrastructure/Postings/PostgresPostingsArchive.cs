using DialDen.RLS.Postings;
using Npgsql;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class PostgresPostingsArchive : IPostingsArchive
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresPostingsArchive(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<int> Add(Posting posting, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO postings (title) VALUES (@title) RETURNING id;",
            connection);
        command.Parameters.AddWithValue("title", posting.Title);

        return (int)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("PostgreSQL did not return the new posting ID."));
    }

    public async Task<Posting?> FindById(int id, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT title FROM postings WHERE id = @id;",
            connection);
        command.Parameters.AddWithValue("id", id);

        var title = await command.ExecuteScalarAsync(cancellationToken);
        return title is string postingTitle ? new Posting(postingTitle) : null;
    }
}
