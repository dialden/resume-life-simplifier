using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.API;

public sealed record UpdatePostingRequest(PostingStatus Status)
{
    public IReadOnlyList<string> JobSiteIds { get; init; } = [];
}
