namespace DialDen.RLS.Abstractions.Postings;

public interface IViewPublicListing
{
    Task<PublicListingResponse?> View(int id, CancellationToken cancellationToken);
}
