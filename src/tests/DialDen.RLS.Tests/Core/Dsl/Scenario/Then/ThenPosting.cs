using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public class ThenPosting<TSuccessResponse, TSuccessVerification>
    where TSuccessVerification : ResponseVerification<TSuccessResponse>
{
    private readonly ThenStage<TSuccessResponse, TSuccessVerification> _stage;
    private readonly List<Action<object>> _verifications = [];

    internal ThenPosting(ThenStage<TSuccessResponse, TSuccessVerification> stage)
    {
        _stage = stage;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasTitle(string expectedTitle)
    {
        _verifications.Add(v =>
        {
            if (v is not CreatePostingVerification creation)
            {
                throw new InvalidOperationException("Posting titles can only be verified when creating a posting.");
            }

            creation.HasTitle(expectedTitle);
        });
        return this;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasStatus(string expectedStatus)
    {
        _verifications.Add(v =>
        {
            if (v is not IPostingStatusVerification posting)
            {
                throw new InvalidOperationException("Posting status can only be verified for a posting operation.");
            }

            posting.HasStatus(expectedStatus);
        });
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasDelivery(string jobSiteId, string expectedStatus)
    {
        _verifications.Add(v =>
        {
            if (v is not IPostingDeliveryVerification posting)
            {
                throw new InvalidOperationException("Deliveries can only be verified for publishing or viewing a posting.");
            }
            posting.HasDelivery(jobSiteId, expectedStatus);
        });
        return this;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> HasNoDeliveries()
    {
        _verifications.Add(v =>
        {
            if (v is not IPostingDeliveryVerification posting)
            {
                throw new InvalidOperationException("Deliveries can only be verified for publishing or viewing a posting.");
            }
            posting.HasNoDeliveries();
        });
        return this;
    }

    private async Task Execute()
    {
        var result = await _stage.GetExecutionResult();
        var verification = result.Result.ShouldSucceed();

        foreach (var check in _verifications)
        {
            check(verification);
        }
    }
}
