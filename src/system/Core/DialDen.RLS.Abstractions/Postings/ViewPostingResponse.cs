namespace DialDen.RLS.Abstractions.Postings;

public sealed record ViewPostingResponse(int Id, string Title, PostingStatus Status)
{
    public string Description { get; init; } = "";
    public Uri? PublicListingUrl { get; init; }
    public IReadOnlyList<JobSitePublicationResponse> JobSitePublications { get; init; } = [];
}
