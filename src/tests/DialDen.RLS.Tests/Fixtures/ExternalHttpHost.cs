namespace DialDen.RLS.Tests.Fixtures;

/// <summary>A host running outside the test process, reached through its URL.</summary>
public sealed class ExternalHttpHost : IHttpHost
{
    private readonly string _url;

    public string Url => _url;

    public ExternalHttpHost(string url)
    {
        _url = url;
    }

    public void Start()
    {
        // The server's lifecycle is managed outside the test process.
    }

    public void Dispose()
    {
    }
}
