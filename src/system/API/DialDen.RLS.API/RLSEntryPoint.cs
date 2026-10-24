namespace DialDen.RLS.API;

/// <summary>The API entry point: the list of actions currently available.</summary>
public sealed record RLSEntryPoint(IReadOnlyList<Link> Links);
