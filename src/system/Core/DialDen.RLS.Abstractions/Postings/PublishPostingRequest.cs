namespace DialDen.RLS.Abstractions.Postings;

public sealed record PublishPostingRequest(int Id)
{
    public IReadOnlyList<string> JobSiteIds { get; init; } = [];
}
