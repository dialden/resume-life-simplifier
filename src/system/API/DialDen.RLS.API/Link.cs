namespace DialDen.RLS.API;

/// <summary>A hypermedia link to an action the client can follow from the current resource.</summary>
public sealed record Link(string Rel, string Href, string Method);
