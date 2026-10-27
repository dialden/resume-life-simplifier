namespace DialDen.RLS.Abstractions.Postings;

/// <summary>Requests a one-based page of ten job postings in identifier order.</summary>
public sealed record ListPostingsRequest
{
    public const int PageSize = 10;

    public int PageNumber { get; }

    public ListPostingsRequest(int pageNumber = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        PageNumber = pageNumber;
    }
}
