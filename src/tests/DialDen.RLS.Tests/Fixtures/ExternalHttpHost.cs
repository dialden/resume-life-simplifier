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
        //TODO : Rework this part
        throw new NotImplementedException("External web server configuration is not implemented.");
    }

    public void Dispose()
    {
    }
}
