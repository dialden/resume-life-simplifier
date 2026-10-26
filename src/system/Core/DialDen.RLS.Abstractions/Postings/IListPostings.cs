namespace DialDen.RLS.Abstractions.Postings;

public interface IListPostings
{
    Task<IReadOnlyList<ViewPostingResponse>> List(CancellationToken cancellationToken);
}
