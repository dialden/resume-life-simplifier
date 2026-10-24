namespace DialDen.RLS.Tests.Core.Clients.API.Endpoints;

public class HealthEndpoint
{
    private readonly HttpClient _httpClient;

    public HealthEndpoint(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<VoidValue, SystemError>> CheckHealth()
    {
        using var response = await _httpClient.GetAsync("/health");
        var healthStatus = (await response.Content.ReadAsStringAsync()).Trim();

        if (response.IsSuccessStatusCode && healthStatus == "Healthy")
        {
            return Result.Success<SystemError>();
        }

        return Result.Failure<SystemError>(SystemError.Of(
            $"RLS API health check returned HTTP {(int)response.StatusCode} ({response.StatusCode}) with status '{healthStatus}'."));
    }
}