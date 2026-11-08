namespace DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

public sealed record ApplicationResponse(int Id, string Name, string Email, string Source, string? ReferralJobSiteId = null);
