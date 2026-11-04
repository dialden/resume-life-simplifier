using MassTransit;

namespace DialDen.RLS.Messaging.Contracts;

/// <summary>Signals that callers should reread a posting to obtain its current state.</summary>
[MessageUrn("DialDen.RLS.Messaging:PostingChanged")]
public sealed record PostingChanged(int PostingId);
