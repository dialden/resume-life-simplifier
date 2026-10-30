namespace DialDen.RLS.Abstractions.Postings;

public sealed record ViewPostingResponse(int Id, string Title, PostingStatus Status)
{
    public IReadOnlyList<JobSitePublicationResponse> JobSitePublications { get; init; } = [];
}
