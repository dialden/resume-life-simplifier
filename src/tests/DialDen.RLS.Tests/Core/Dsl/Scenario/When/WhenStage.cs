namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenStage
{
    private readonly UseCaseDsl _app;
    private readonly ScenarioDsl _scenario;
    private readonly Func<Task> _setup;

    internal WhenStage(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
    {
        _app = app;
        _scenario = scenario;
        _setup = setup;
    }

    public WhenCreatePosting CreatePosting() => new(_app, _scenario, _setup);
}
