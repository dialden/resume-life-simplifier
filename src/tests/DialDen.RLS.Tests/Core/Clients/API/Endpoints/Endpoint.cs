using Microsoft.AspNetCore.Mvc;

namespace DialDen.RLS.Tests.Core.Clients.API.Endpoints;

public abstract class Endpoint
{
    protected JsonHttpClient<ValidationProblemDetails> JsonHttpClient {get;}

    protected Endpoint(JsonHttpClient<ValidationProblemDetails> jsonHttpClient)
    {
        JsonHttpClient = jsonHttpClient;
    }
}