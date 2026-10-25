namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Then;

public class ThenSuccessAnd<TSuccessResponse, TSuccessVerification>
    where TSuccessVerification : ResponseVerification<TSuccessResponse>
{
    private readonly ThenStage<TSuccessResponse, TSuccessVerification> _stage;

    internal ThenSuccessAnd(ThenStage<TSuccessResponse, TSuccessVerification> stage)
    {
        _stage = stage;
    }

    public ThenPosting<TSuccessResponse, TSuccessVerification> Posting() => new(_stage);

}
