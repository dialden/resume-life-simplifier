using MassTransit;

namespace DialDen.RLS.Messaging.Contracts;

[MessageUrn("DialDen.RLS.Messaging:ApplicationChanged")]
public sealed record ApplicationChanged(int PostingId, int ApplicationId);
