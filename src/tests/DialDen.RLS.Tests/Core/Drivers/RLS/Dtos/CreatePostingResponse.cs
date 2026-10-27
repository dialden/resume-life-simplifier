namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public record CreatePostingResponse(
    int? Id,
    string Title,
    DialDen.RLS.Abstractions.Postings.PostingStatus Status);
