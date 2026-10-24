using DialDen.RLS.Tests.Core.Drivers.RLS;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class GoToRLS : RLSUseCase<VoidValue, VoidVerification>
{
    public GoToRLS(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public override async Task<RLSUseCaseResult<VoidValue, VoidVerification>> Execute()
    {
        var result = await Driver.GoToRLS();
        return new RLSUseCaseResult<VoidValue, VoidVerification>(
            result,
            Context,
            (response, ctx) => new VoidVerification(response, ctx));
    }
}
