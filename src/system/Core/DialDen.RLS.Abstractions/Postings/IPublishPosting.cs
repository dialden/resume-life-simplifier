namespace DialDen.RLS.Abstractions.Postings;

public interface IPublishPosting
{
    Task<PublishPostingResponse?> Publish(PublishPostingRequest request, CancellationToken cancellationToken);
}
