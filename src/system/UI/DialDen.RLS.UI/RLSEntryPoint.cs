namespace DialDen.RLS.UI;

/// <summary>A hypermedia link returned by the RLS API.</summary>
public sealed record Link(string Rel, string Href, string Method);

/// <summary>The RLS API entry point: the actions currently available.</summary>
public sealed record RLSEntryPoint(IReadOnlyList<Link> Links);
