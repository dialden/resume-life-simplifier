namespace DialDen.RLS.Pipelines;

public static class PipelineLog
{
    public static void Write(string message) =>
        Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss'Z'}] {message}");
}
