using DialDen.RLS.Messaging.Contracts;
using DialDen.RLS.Postings;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DialDen.RLS.Infrastructure.Publishing;

internal sealed class OutboxJobSitePublicationRequests(
    ISendEndpointProvider sendEndpointProvider,
    IOptions<MessagingOptions> messagingOptions) : IJobSitePublicationRequests
{
    public async Task Request(JobSitePublicationRequest request, CancellationToken cancellationToken)
    {
        var endpoint = await sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{messagingOptions.Value.JobSitePublicationQueue}"));
        await endpoint.Send(
            new PublishPostingOnJobSite(request.PostingId, request.JobSiteId, request.PublicListingUrl),
            cancellationToken);
    }
}
