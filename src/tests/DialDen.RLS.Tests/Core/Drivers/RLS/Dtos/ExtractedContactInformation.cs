namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record ExtractedContactInformation(
    string? Name, string? Email, string? PhoneNumber, string? PostalAddress, string? LinkedInUrl);
