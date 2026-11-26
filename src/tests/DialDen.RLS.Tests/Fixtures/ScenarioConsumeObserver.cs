using MassTransit;

namespace DialDen.RLS.Tests.Fixtures;

internal sealed class ScenarioConsumeObserver(ScenarioDiagnostics diagnostics) : IConsumeObserver
{
    public Task PreConsume<T>(ConsumeContext<T> context) where T : class
    {
        diagnostics.Record("Worker", $"Consume started: {typeof(T).FullName}, message={context.MessageId}");
        return Task.CompletedTask;
    }

    public Task PostConsume<T>(ConsumeContext<T> context) where T : class
    {
        diagnostics.Record("Worker", $"Consume completed: {typeof(T).FullName}, message={context.MessageId}");
        return Task.CompletedTask;
    }

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
    {
        var types = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
            types.Add(current.GetType().FullName ?? current.GetType().Name);
        diagnostics.Record("Worker", $"Consume faulted: {typeof(T).FullName}, message={context.MessageId}, " +
            $"exceptions={string.Join(" -> ", types)}");
        return Task.CompletedTask;
    }
}
