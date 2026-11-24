namespace DialDen.RLS.Abstractions.Postings;

public sealed record CreatePostingResponse(int Id, string Title, PostingStatus Status)
{
    public string Description { get; init; } = "";
    public string? RequiredEducation { get; init; }
}
