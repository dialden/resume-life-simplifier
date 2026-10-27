using DialDen.RLS.Postings;
using Npgsql;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class PostgresJobPostings : IPostingsArchive
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresJobPostings(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Posting> Add(string title, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO postings (title) VALUES (@title) RETURNING id;",
            connection);
        command.Parameters.AddWithValue("title", title);

        var postingId = (int)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("PostgreSQL did not return the new posting ID."));
        return new Posting(postingId, title);
    }

    public async Task<Posting?> FindById(int postingId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT title FROM postings WHERE id = @id;",
            connection);
        command.Parameters.AddWithValue("id", postingId);

        var title = await command.ExecuteScalarAsync(cancellationToken);
        return title is string postingTitle ? new Posting(postingId, postingTitle) : null;
    }

    public async Task<PostingList> List(int pageNumber, int postingsPerPage, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(postingsPerPage);
        var offset = ((long)pageNumber - 1) * postingsPerPage;
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT page.id, page.title, total.count
            FROM (SELECT COUNT(*) AS count FROM postings) AS total
            LEFT JOIN LATERAL (
                SELECT id, title FROM postings ORDER BY id LIMIT @limit OFFSET @offset
            ) AS page ON TRUE
            ORDER BY page.id;
            """,
            connection);
        command.Parameters.AddWithValue("limit", postingsPerPage);
        command.Parameters.AddWithValue("offset", offset);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("PostgreSQL did not return the total posting count.");
        }

        var totalPostings = reader.GetInt64(2);
        var postings = new List<Posting>();
        do
        {
            if (!reader.IsDBNull(0))
            {
                postings.Add(new Posting(reader.GetInt32(0), reader.GetString(1)));
            }
        }
        while (await reader.ReadAsync(cancellationToken));

        return new PostingList(postings, totalPostings);
    }
}
