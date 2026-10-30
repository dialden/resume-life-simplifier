namespace DialDen.RLS.Abstractions.Postings;

public sealed record PublishPostingResponse(int Id, PostingStatus Status)
{
    public IReadOnlyList<JobSitePublicationResponse> JobSitePublications { get; init; } = [];
}
