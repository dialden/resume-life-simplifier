using DialDen.RLS.Tests.Core.Drivers.RLS;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public class GivenLanguage : BaseGiven
{
    private readonly Language _language;

    internal GivenLanguage(GivenStage givenStage, Language language) : base(givenStage)
    {
        _language = language;
    }

    internal override async Task Execute(UseCaseDsl app)
    {
        var result = await app.RLS.ChooseLanguage().To(_language).Execute();
        result.ShouldSucceed();
    }
}
