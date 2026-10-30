namespace DialDen.RLS.Worker.Root;

internal static class Program
{
    private static Task Main(string[] args) => WorkerHost.CreateBuilder(args).Build().RunAsync();
}
