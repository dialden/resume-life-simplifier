namespace DialDen.RLS.Abstractions.Postings;

public sealed record PublishPostingResponse(int Id, PostingStatus Status, Uri PublicListingUrl)
{
    public IReadOnlyList<JobSitePublicationResponse> JobSitePublications { get; init; } = [];
}
