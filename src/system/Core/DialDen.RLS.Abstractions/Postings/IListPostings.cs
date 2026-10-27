namespace DialDen.RLS.Abstractions.Postings;

public interface IListPostings
{
    Task<ListPostingsResponse> List(ListPostingsRequest request, CancellationToken cancellationToken);
}
