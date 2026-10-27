namespace DialDen.RLS.Abstractions.Postings;

public sealed record CreatePostingResponse(int Id, string Title, PostingStatus Status);
