using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public sealed record PostingDelivery(string JobSiteId, PostingDeliveryStatus Status);
