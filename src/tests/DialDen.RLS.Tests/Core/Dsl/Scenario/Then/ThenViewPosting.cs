using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public class ThenViewPosting<TSuccessResponse, TSuccessVerification>
    where TSuccessVerification : ResponseVerification<TSuccessResponse>
{
    private readonly ThenStage<TSuccessResponse, TSuccessVerification> _stage;
    private readonly List<Action<ViewPostingVerification>> _verifications = [];

    internal ThenViewPosting(ThenStage<TSuccessResponse, TSuccessVerification> stage)
    {
        _stage = stage;
    }

    public ThenViewPosting<TSuccessResponse, TSuccessVerification> HasTitle(string expectedTitle)
    {
        _verifications.Add(verification => verification.HasTitle(expectedTitle));
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    private async Task Execute()
    {
        var result = await _stage.GetExecutionResult();
        if (result.Result.ShouldSucceed() is not CreatePostingVerification createdPosting)
        {
            throw new InvalidOperationException("ViewPosting() requires the result of a CreatePosting operation.");
        }

        var postingId = createdPosting.Id
            ?? throw new InvalidOperationException("The CreatePosting result did not include a posting ID.");
        var viewResult = await _stage.App.RLS.ViewPosting().WithId(postingId).Execute();
        var verification = viewResult.ShouldSucceed();
        verification.HasId(postingId);

        foreach (var check in _verifications)
        {
            check(verification);
        }
    }
}
