using DialDen.RLS.JobApplications;
using DialDen.RLS.Messaging.Contracts;
using MassTransit;

namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class OutboxApplicationChanges(IPublishEndpoint publisher) : IApplicationChanges
{
    public Task Record(int postingId, int applicationId, CancellationToken cancellationToken) =>
        publisher.Publish(new ApplicationChanged(postingId, applicationId), cancellationToken);
}
