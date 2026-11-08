using DialDen.RLS.Tests.Core.Clients.API.Endpoints;
namespace DialDen.RLS.Tests.Core.Clients.API;

public class APIClient
{
    public APIClient(HttpClient httpClient)
    {
        Health = new HealthEndpoint(httpClient);
        Postings = new PostingsEndpoint(httpClient);
        Applications = new ApplicationsEndpoint(httpClient);
    }

    public HealthEndpoint Health { get; }
    public PostingsEndpoint Postings { get; }
    public ApplicationsEndpoint Applications { get; }
}
