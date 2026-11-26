using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public sealed class ThenCandidateComparison<TResponse, TVerification>(ThenStage<TResponse, TVerification> stage)
    where TVerification : ResponseVerification<TResponse>
{
    private string _requirement = "";
    private string? _evidence;
    private string[] _names = [];

    public ThenCandidateComparison<TResponse, TVerification> ShowsEducationEvidence(
        string requirement, string? evidence, params string[] names)
    {
        _requirement = requirement;
        _evidence = evidence;
        _names = names;
        return this;
    }

    public TaskAwaiter GetAwaiter() => stage.ExecuteAndDispose(Execute).GetAwaiter();

    private async Task Execute()
    {
        var execution = await stage.GetExecutionResult();
        if (execution.Result.ShouldSucceed() is not ComparisonVerification comparison)
            throw new InvalidOperationException("CandidateComparison requires comparing candidates.");
        comparison.ShowsEducationEvidence(_requirement, _evidence, _names);
    }
}
