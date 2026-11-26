using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public sealed class ThenEducationReview<TResponse, TVerification>(ThenStage<TResponse, TVerification> stage)
    where TVerification : ResponseVerification<TResponse>
{
    private string _group = "";
    private bool? _verdict;
    private string _note = "";

    public ThenEducationReview<TResponse, TVerification> IsSaved(string group, bool? verdict, string note)
    {
        _group = group;
        _verdict = verdict;
        _note = note;
        return this;
    }

    public TaskAwaiter GetAwaiter() => stage.ExecuteAndDispose(Execute).GetAwaiter();

    private async Task Execute()
    {
        var execution = await stage.GetExecutionResult();
        if (execution.Result.ShouldSucceed() is not EducationReviewVerification review)
            throw new InvalidOperationException("EducationReview requires reviewing education.");
        await review.IsSaved(_group, _verdict, _note);
    }
}
