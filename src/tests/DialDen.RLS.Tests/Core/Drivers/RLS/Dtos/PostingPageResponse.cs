namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record PostingPageResponse(
    IReadOnlyList<ViewPostingResponse> Items, long TotalCount, int PageNumber, int PageSize);
