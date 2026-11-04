using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public sealed class ThenOverview<TResponse, TVerification>
    where TVerification : ResponseVerification<TResponse>
{
    private readonly ThenStage<TResponse, TVerification> _stage;
    private readonly List<Action<ViewOverviewVerification>> _checks = [];

    internal ThenOverview(ThenStage<TResponse, TVerification> stage) => _stage = stage;

    public ThenOverview<TResponse, TVerification> HasTitle(string expectedTitle)
    {
        _checks.Add(verification => verification.HasTitle(expectedTitle));
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    private async Task Execute()
    {
        var result = await _stage.GetExecutionResult();
        if (result.Result.ShouldSucceed() is not ViewOverviewVerification verification)
        {
            throw new InvalidOperationException("Overview() requires a ViewOverview operation.");
        }
        foreach (var check in _checks)
        {
            check(verification);
        }
    }
}
