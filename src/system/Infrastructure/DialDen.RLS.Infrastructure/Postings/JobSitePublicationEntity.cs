using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Infrastructure.Postings;

internal sealed class JobSitePublicationEntity
{
    public int PostingId { get; set; }

    public required string JobSiteId { get; set; }

    public JobSitePublicationStatus Status { get; set; } = JobSitePublicationStatus.Pending;
}
