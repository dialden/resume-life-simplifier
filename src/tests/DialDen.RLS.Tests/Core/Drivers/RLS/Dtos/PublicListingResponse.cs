namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record PublicListingResponse(int Id, string Title, string Description, string? RequiredEducation = null)
{
    public Uri? PublicListingUrl { get; init; }
}
