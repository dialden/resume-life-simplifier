namespace DialDen.RLS.Abstractions.Postings;

public interface ICreatePosting
{
    Task<CreatePostingResponse> Create(CreatePostingRequest request, CancellationToken cancellationToken);
}
