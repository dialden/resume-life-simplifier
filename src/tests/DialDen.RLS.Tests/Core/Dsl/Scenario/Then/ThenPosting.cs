using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public class ThenPosting<TSuccessResponse, TSuccessVerification>
    where TSuccessVerification : ResponseVerification<TSuccessResponse>
{
    private readonly ThenStage<TSuccessResponse, TSuccessVerification> _stage;
    private readonly List<Action<CreatePostingVerification>> _verifications = [];

    internal ThenPosting(ThenStage<TSuccessResponse, TSuccessVerification> stage)
    {
        _stage = stage;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasTitle(string expectedTitle)
    {
        _verifications.Add(v => v.HasTitle(expectedTitle));
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    private async Task Execute()
    {
        var result = await _stage.GetExecutionResult();
        if (result.Result.ShouldSucceed() is not CreatePostingVerification verification)
        {
            throw new InvalidOperationException("Posting() can only verify the result of a CreatePosting operation.");
        }

        foreach (var check in _verifications)
        {
            check(verification);
        }
    }
}
