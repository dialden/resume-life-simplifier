namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record PublishPostingResponse(
    int Id,
    DialDen.RLS.Abstractions.Postings.PostingStatus Status)
{
    public IReadOnlyList<JobSitePublicationResponse> JobSitePublications { get; init; } = [];
}
