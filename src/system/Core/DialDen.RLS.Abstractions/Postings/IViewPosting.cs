namespace DialDen.RLS.Abstractions.Postings;

public interface IViewPosting
{
    Task<ViewPostingResponse?> View(ViewPostingRequest request, CancellationToken cancellationToken);
}
