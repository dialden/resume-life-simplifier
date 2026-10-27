namespace DialDen.RLS.Abstractions.Postings;

/// <summary>A page of job postings and the total size of the opportunity library.</summary>
public sealed record ListPostingsResponse(
    IReadOnlyList<ViewPostingResponse> Items,
    long TotalCount,
    int PageNumber,
    int PageSize);
