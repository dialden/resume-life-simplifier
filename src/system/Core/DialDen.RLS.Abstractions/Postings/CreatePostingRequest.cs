namespace DialDen.RLS.Abstractions.Postings;

/// <summary>Creates a posting with its title, required description, and optional education requirement.</summary>
public sealed record CreatePostingRequest(string Title, string Description, string? RequiredEducation = null);
