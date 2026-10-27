using DialDen.RLS.Abstractions.Postings;
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
            "INSERT INTO postings (title) VALUES (@title) RETURNING id, status;",
            connection);
        command.Parameters.AddWithValue("title", title);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("PostgreSQL did not return the new posting.");
        }

        return new Posting(reader.GetInt32(0), title, Enum.Parse<PostingStatus>(reader.GetString(1), ignoreCase: false));
    }

    public async Task<Posting?> FindById(int postingId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT title, status FROM postings WHERE id = @id;",
            connection);
        command.Parameters.AddWithValue("id", postingId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new Posting(postingId, reader.GetString(0), Enum.Parse<PostingStatus>(reader.GetString(1), ignoreCase: false))
            : null;
    }

    public async Task<Posting?> Publish(int postingId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE postings SET status = 'Published'
            WHERE id = @id
            RETURNING title, status;
            """,
            connection);
        command.Parameters.AddWithValue("id", postingId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new Posting(postingId, reader.GetString(0), Enum.Parse<PostingStatus>(reader.GetString(1), ignoreCase: false))
            : null;
    }

    public async Task<PostingList> List(int pageNumber, int postingsPerPage, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(postingsPerPage);
        var offset = ((long)pageNumber - 1) * postingsPerPage;
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT page.id, page.title, page.status, total.count
            FROM (SELECT COUNT(*) AS count FROM postings) AS total
            LEFT JOIN LATERAL (
                SELECT id, title, status FROM postings ORDER BY id LIMIT @limit OFFSET @offset
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

        var totalPostings = reader.GetInt64(3);
        var postings = new List<Posting>();
        do
        {
            if (!reader.IsDBNull(0))
            {
                postings.Add(new Posting(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    Enum.Parse<PostingStatus>(reader.GetString(2), ignoreCase: false)));
            }
        }
        while (await reader.ReadAsync(cancellationToken));

        return new PostingList(postings, totalPostings);
    }
}
