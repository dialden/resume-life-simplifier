using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public sealed class ThenReviewedApplications<TResponse, TVerification>(
    ThenStage<TResponse, TVerification> stage)
    where TVerification : ResponseVerification<TResponse>
{
    private string? _expected;
    private bool _failed;

    public ThenReviewedApplications<TResponse, TVerification> UpdatesToFailedExtraction()
    {
        _failed = true;
        return this;
    }

    public ThenReviewedApplications<TResponse, TVerification> UpdatesToExtractedEmail(string? email)
    {
        _expected = email;
        return this;
    }

    public TaskAwaiter GetAwaiter() => stage.ExecuteAndDispose(Execute).GetAwaiter();

    private async Task Execute()
    {
        var execution = await stage.GetExecutionResult();
        if (execution.Result.ShouldSucceed() is not ApplicationReviewVerification review)
            throw new InvalidOperationException("ReviewedApplications requires completing resume analysis.");
        if (_failed)
            await review.UpdatesToFailedExtraction();
        else
            await review.UpdatesToExtractedEmail(_expected);
    }
}
