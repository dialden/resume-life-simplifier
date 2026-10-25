using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public class WhenViewPosting : BaseWhen<ViewPostingResponse, ViewPostingVerification>
{
    private string _idAlias = ScenarioDefaults.PostingId;

    internal WhenViewPosting(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
        : base(app, scenario, setup)
    {
    }

    public WhenViewPosting WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    protected override async Task<ExecutionResult<ViewPostingResponse, ViewPostingVerification>> Execute(UseCaseDsl app)
    {
        var result = await app.RLS.ViewPosting().WithId(_idAlias).Execute();
        return new ExecutionResult<ViewPostingResponse, ViewPostingVerification>(result);
    }
}
