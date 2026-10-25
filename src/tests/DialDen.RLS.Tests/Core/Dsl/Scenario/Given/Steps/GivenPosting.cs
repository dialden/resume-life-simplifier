namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public class GivenPosting : BaseGiven
{
    private string _idAlias = ScenarioDefaults.PostingId;
    private string _title = "Software Engineer";

    internal GivenPosting(GivenStage givenStage) : base(givenStage)
    {
    }

    public GivenPosting WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    public GivenPosting WithTitle(string title)
    {
        _title = title;
        return this;
    }

    internal override async Task Execute(UseCaseDsl app)
    {
        var result = await app.RLS.CreatePosting().WithTitle(_title).Execute();
        result.ShouldSucceed().SaveIdAs(_idAlias);
    }
}
