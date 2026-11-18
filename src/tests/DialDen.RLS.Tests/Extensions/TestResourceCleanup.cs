using System.Runtime.ExceptionServices;

namespace DialDen.RLS.Tests.Extensions;

internal static class TestResourceCleanup
{
    internal static ValueTask RunAsync(params Func<ValueTask>[] actions) => RunAsync(null, actions);

    internal static async ValueTask RunAsync(Exception? failure, params Func<ValueTask>[] actions)
    {
        var failures = new List<Exception>();
        if (failure is not null)
            failures.Add(failure);
        foreach (var action in actions)
        {
            try
            {
                await action();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Test-resource cleanup failed.", failures);
    }
}
