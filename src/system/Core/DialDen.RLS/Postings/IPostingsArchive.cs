namespace DialDen.RLS.Postings;

public interface IPostingsArchive
{
    Task<Posting> Add(string title, CancellationToken cancellationToken);

    Task<Posting?> FindById(int postingId, CancellationToken cancellationToken);

    Task<Posting?> Publish(int postingId, IReadOnlyList<string> jobSiteIds, CancellationToken cancellationToken);

    Task<PostingList> List(int pageNumber, int postingsPerPage, CancellationToken cancellationToken);
}
