using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewOverview : RLSUseCase<OverviewResponse, ViewOverviewVerification>
{
    public ViewOverview(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public override async Task<RLSUseCaseResult<OverviewResponse, ViewOverviewVerification>> Execute()
    {
        var result = await Driver.ViewOverview();
        return new RLSUseCaseResult<OverviewResponse, ViewOverviewVerification>(
            result,
            Context,
            (response, ctx) => new ViewOverviewVerification(response, ctx));
    }
}
