using MassTransit;

namespace DialDen.RLS.Messaging.Contracts;

// The URN is stored with every queued command, so it is pinned to survive renames and moves.
[MessageUrn("DialDen.RLS.Messaging:PublishPostingOnJobSite")]
public sealed record PublishPostingOnJobSite(int PostingId, string JobSiteId);
