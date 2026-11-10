using DialDen.RLS.JobApplications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class OutboxApplicationReceipts(IPublishEndpoint publishEndpoint) : IApplicationReceipts
{
    public Task Record(ApplicationReceipt receipt, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new ApplicationReceived(receipt.ApplicationId, receipt.PostingId), cancellationToken);
}
