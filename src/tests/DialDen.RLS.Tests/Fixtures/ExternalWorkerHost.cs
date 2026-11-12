namespace DialDen.RLS.Tests.Fixtures;

public sealed class ExternalWorkerHost : IWorkerHost
{
    public void Start()
    {
        // The worker's lifecycle is managed outside the test process.
    }

    public void Dispose()
    {
    }
}
