using System.Runtime.CompilerServices;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public sealed class ThenPostingPages<TResponse, TVerification>
    where TVerification : ResponseVerification<TResponse>
{
    private readonly ThenStage<TResponse, TVerification> _stage;
    private readonly List<Action<ListPostingsVerification>> _checks = [];

    internal ThenPostingPages(ThenStage<TResponse, TVerification> stage) => _stage = stage;

    public ThenPostingPages<TResponse, TVerification> IncludesPostings(string alias, int count)
    {
        _checks.Add(verification => verification.IncludesPostings(alias, count));
        return this;
    }

    public ThenPostingPages<TResponse, TVerification> HasEmptyPage(int pageNumber)
    {
        _checks.Add(verification => verification.HasEmptyPage(pageNumber));
        return this;
    }

    public ThenPostingPages<TResponse, TVerification> HasPostingStatus(string alias, string expectedStatus)
    {
        _checks.Add(verification => verification.HasPostingStatus(alias, expectedStatus));
        return this;
    }

    public TaskAwaiter GetAwaiter() => Execute().GetAwaiter();

    public ThenPostingPages<TResponse, TVerification> HasPostingDelivery(
        string alias, string jobSiteId, string expectedStatus)
    {
        _checks.Add(verification => verification.HasPostingDelivery(alias, jobSiteId, expectedStatus));
        return this;
    }

    private async Task Execute()
    {
        var result = await _stage.GetExecutionResult();
        if (result.Result.ShouldSucceed() is not ListPostingsVerification verification)
        {
            throw new InvalidOperationException("PostingPages() requires a ListPostings operation.");
        }
        foreach (var check in _checks)
        {
            check(verification);
        }
    }
}
