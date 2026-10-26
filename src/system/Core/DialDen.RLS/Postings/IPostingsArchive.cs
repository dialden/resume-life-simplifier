namespace DialDen.RLS.Postings;

public interface IPostingsArchive
{
    Task<int> Add(Posting posting, CancellationToken cancellationToken);

    Task<Posting?> FindById(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ArchivedPosting>> List(CancellationToken cancellationToken);
}
