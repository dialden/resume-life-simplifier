namespace DialDen.RLS.Postings;

public interface IPostingsArchive
{
    Task<Posting> Add(string title, CancellationToken cancellationToken);

    Task<Posting?> FindById(int postingId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a posting found through <see cref="FindById"/>.
    /// </summary>
    /// <exception cref="PostingChangedConcurrentlyException">
    /// The stored posting changed after it was found.
    /// </exception>
    Task Save(Posting posting, CancellationToken cancellationToken);

    Task<PostingList> List(int pageNumber, int postingsPerPage, CancellationToken cancellationToken);
}
