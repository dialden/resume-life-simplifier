namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record PublicListingResponse(int Id, string Title, string Description)
{
    public Uri? PublicListingUrl { get; init; }
}
