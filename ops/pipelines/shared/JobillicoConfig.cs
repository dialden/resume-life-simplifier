namespace DialDen.RLS.Pipelines;

public sealed record JobillicoConfig(string Endpoint, string RecruiterId, string LocationId, bool UseProduction)
{
    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException(
                "The googleCloud.jobillico.endpoint must be an absolute HTTPS URL without credentials, query or fragment.");
        }
        if (!long.TryParse(RecruiterId, out var recruiterId) || recruiterId <= 0 ||
            !long.TryParse(LocationId, out var locationId) || locationId <= 0)
        {
            throw new InvalidOperationException(
                "The googleCloud.jobillico recruiterId and locationId must be positive integers.");
        }
    }
}
