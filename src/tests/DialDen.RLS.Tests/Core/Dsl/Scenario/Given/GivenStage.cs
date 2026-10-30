using DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given;

/// <summary>
/// Collects the preconditions. They are applied in Setup(), which runs lazily when the Then chain is awaited.
/// </summary>
public class GivenStage
{
    private readonly UseCaseDsl _app;
    private readonly ScenarioDsl _scenario;
    private readonly List<BaseGiven> _steps = [];

    internal GivenStage(UseCaseDsl app, ScenarioDsl scenario)
    {
        _app = app;
        _scenario = scenario;
    }

    public GivenStage And() => this;

    public GivenPosting Posting()
    {
        var step = new GivenPosting(this);
        _steps.Add(step);
        return step;
    }

    public GivenJobSite JobSite(string jobSiteId)
    {
        var step = new GivenJobSite(this, jobSiteId);
        _steps.Add(step);
        return step;
    }

    internal IReadOnlyList<string> PostingsPublishedTo(string jobSiteId) =>
        _steps.OfType<GivenPosting>().SelectMany(step => step.AliasesPublishedTo(jobSiteId)).ToList();

    public When.WhenStage When() => new(_app, _scenario, Setup);

    internal async Task Setup()
    {
        foreach (var step in _steps)
        {
            await step.Execute(_app);
        }
    }
}
