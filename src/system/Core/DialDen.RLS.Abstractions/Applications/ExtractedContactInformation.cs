namespace DialDen.RLS.Abstractions.Applications;

public sealed record ExtractedContactInformation(
    string? Name, string? Email, string? PhoneNumber, string? PostalAddress, string? LinkedInUrl);
