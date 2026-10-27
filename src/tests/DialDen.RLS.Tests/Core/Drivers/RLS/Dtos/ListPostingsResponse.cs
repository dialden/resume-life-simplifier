namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record ListPostingsResponse(IReadOnlyList<PostingPageResponse> Pages);
