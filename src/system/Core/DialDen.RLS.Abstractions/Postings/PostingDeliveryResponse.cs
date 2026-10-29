namespace DialDen.RLS.Abstractions.Postings;

public sealed record PostingDeliveryResponse(string JobSiteId, PostingDeliveryStatus Status);
