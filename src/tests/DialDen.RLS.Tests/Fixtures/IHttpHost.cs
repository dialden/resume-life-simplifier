namespace DialDen.RLS.Tests.Fixtures;

/// <summary>An HTTP host under test, either running in the test process or external to it.</summary>
public interface IHttpHost : IDisposable
{
    /// <summary>The host base URL. Only available once <see cref="Start"/> has been called.</summary>
    string Url { get; }

    /// <summary>Starts the host. Must be idempotent and thread-safe.</summary>
    void Start();
}
