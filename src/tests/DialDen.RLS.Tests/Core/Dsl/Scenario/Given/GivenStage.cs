using DialDen.RLS.Tests.Core.Drivers.RLS;
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
    private bool _languageChosen;

    internal GivenStage(UseCaseDsl app, ScenarioDsl scenario)
    {
        _app = app;
        _scenario = scenario;
    }

    public GivenStage And() => this;

    /// <summary>The customer works in the given language. Scenarios use English unless they say otherwise.</summary>
    public GivenStage Language(Language language)
    {
        _languageChosen = true;
        _steps.Add(new GivenLanguage(this, language));
        return this;
    }

    /// <summary>The customer has not chosen a language, so the system's default applies.</summary>
    public GivenStage NoLanguageChosen()
    {
        _languageChosen = true;
        return this;
    }

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

    public When.WhenStage When() => new(_app, _scenario, Setup);

    internal async Task Setup()
    {
        await _app.ExternalSystems.JobSites.Reset();
        if (!_languageChosen)
        {
            (await _app.RLS.ChooseLanguage().To(Drivers.RLS.Language.English).Execute()).ShouldSucceed();
        }
        foreach (var step in _steps)
        {
            await step.Execute(_app);
        }
    }
}
