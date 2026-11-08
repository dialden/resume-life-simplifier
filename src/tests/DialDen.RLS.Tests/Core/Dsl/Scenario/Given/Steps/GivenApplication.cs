using DialDen.RLS.Tests.Core.Dsl.Scenario.When;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public sealed class GivenApplication(GivenStage stage) : BaseGiven(stage)
{
    private string _email = "alex@example.com";

    public GivenApplication WithEmail(string email)
    {
        _email = email;
        return this;
    }

    internal override async Task Execute(UseCaseDsl app)
    {
        var result = await app.RLS.ApplyToPosting()
            .WithId(ScenarioDefaults.PostingId)
            .WithApplicant("Alex Example", _email)
            .WithResume(SampleResume.Create())
            .Execute();
        result.ShouldSucceed();
    }
}
