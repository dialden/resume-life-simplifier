namespace DialDen.RLS.Abstractions.Postings;

/// <summary>Creates a posting with its title and required description.</summary>
public sealed record CreatePostingRequest(string Title, string Description);
