using DialDen.RLS.Tests.Core.Clients.API.Endpoints;
namespace DialDen.RLS.Tests.Core.Clients.API;

public class APIClient
{
    private readonly HttpClient _httpClient;
    public APIClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        Health = new HealthEndpoint(httpClient);
        Postings = new PostingsEndpoint(httpClient);
    }

    public HealthEndpoint Health { get; }
    public PostingsEndpoint Postings { get; }

    /// <summary>Asks the API to answer in the given language tag, e.g. "fr".</summary>
    public void AcceptLanguage(string languageTag)
    {
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
        _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new(languageTag));
    }
}
