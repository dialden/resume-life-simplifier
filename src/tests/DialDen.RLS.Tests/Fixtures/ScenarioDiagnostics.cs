using System.Diagnostics;
using System.Text;

namespace DialDen.RLS.Tests.Fixtures;

internal sealed class ScenarioDiagnostics
{
    private const int Capacity = 500;
    private readonly object _lock = new();
    private readonly Queue<string> _entries = new();
    private int _discarded;

    public void Record(string source, string message)
    {
        lock (_lock)
        {
            if (_entries.Count == Capacity)
            {
                _entries.Dequeue();
                _discarded++;
            }
            _entries.Enqueue($"{DateTimeOffset.UtcNow:O} {source}: {message}");
        }
    }

    public string Snapshot()
    {
        using var process = Process.GetCurrentProcess();
        ThreadPool.GetAvailableThreads(out var availableWorkers, out _);
        var result = new StringBuilder()
            .AppendLine("Scenario diagnostics (UTC; values and request bodies omitted)")
            .AppendLine($"Process: working set {process.WorkingSet64} bytes, CPU time {process.TotalProcessorTime}, " +
                        $"threads {process.Threads.Count}, processor count {Environment.ProcessorCount}")
            .AppendLine($"Thread pool: threads {ThreadPool.ThreadCount}, available workers {availableWorkers}, " +
                        $"pending work items {ThreadPool.PendingWorkItemCount}");
        lock (_lock)
        {
            if (_discarded > 0)
                result.AppendLine($"Earlier entries discarded: {_discarded}");
            foreach (var entry in _entries)
                result.AppendLine(entry);
        }
        return result.ToString();
    }

    public void WriteToTestOutput()
    {
        var output = Snapshot();
        if (TestContext.Current.TestOutputHelper is { } helper)
            helper.WriteLine(output);
        else
            Console.Error.WriteLine(output);
    }
}
