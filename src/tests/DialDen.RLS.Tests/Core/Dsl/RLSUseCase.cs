using DialDen.RLS.Tests.Core.Drivers.RLS;

namespace DialDen.RLS.Tests.Core.Dsl;

public abstract class RLSUseCase<TResponse, TVerification>
    where TVerification : ResponseVerification<TResponse>
{
    protected IRLSDriver Driver { get; }
    protected UseCaseContext Context { get; }

    protected RLSUseCase(IRLSDriver driver, UseCaseContext context)
    {
        Driver = driver;
        Context = context;
    }

    public abstract Task<RLSUseCaseResult<TResponse, TVerification>> Execute();
}
