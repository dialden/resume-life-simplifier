namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public record ViewPostingResponse(
    int Id,
    string Title,
    DialDen.RLS.Abstractions.Postings.PostingStatus Status)
{
    public string Description { get; init; } = "";
    public IReadOnlyList<JobSitePublicationResponse> JobSitePublications { get; init; } = [];
}
