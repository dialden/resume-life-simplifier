using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenCompareCandidates(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup, string[] names)
    : BaseWhen<ComparisonResponse, ComparisonVerification>(app, scenario, setup)
{
    protected override async Task<ExecutionResult<ComparisonResponse, ComparisonVerification>> Execute(UseCaseDsl app) =>
        new(await app.RLS.CompareCandidates().Named(names).Execute());
}
