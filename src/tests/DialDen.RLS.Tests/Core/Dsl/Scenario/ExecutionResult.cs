namespace DialDen.RLS.Tests.Core.Dsl.Scenario;

/// <summary>The outcome of the When step, kept so the Then stage can verify it.</summary>
public class ExecutionResult<TSuccessResponse, TSuccessVerification>
    where TSuccessVerification : ResponseVerification<TSuccessResponse>
{
    public ExecutionResult(RLSUseCaseResult<TSuccessResponse, TSuccessVerification> result)
    {
        Result = result;
    }

    public RLSUseCaseResult<TSuccessResponse, TSuccessVerification> Result { get; }
}
