using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public class WhenViewOverview : BaseWhen<OverviewResponse, ViewOverviewVerification>
{
    internal WhenViewOverview(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
        : base(app, scenario, setup)
    {
    }

    protected override async Task<ExecutionResult<OverviewResponse, ViewOverviewVerification>> Execute(UseCaseDsl app)
    {
        var result = await app.RLS.ViewOverview().Execute();
        return new ExecutionResult<OverviewResponse, ViewOverviewVerification>(result);
    }
}
