using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public class WhenCreatePosting : BaseWhen<CreatePostingResponse, CreatePostingVerification>
{
    private string _title = "Software Engineer";

    internal WhenCreatePosting(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
        : base(app, scenario, setup)
    {
    }

    public WhenCreatePosting WithTitle(string title)
    {
        _title = title;
        return this;
    }

    protected override async Task<ExecutionResult<CreatePostingResponse, CreatePostingVerification>> Execute(UseCaseDsl app)
    {
        var result = await app.RLS.CreatePosting().WithTitle(_title).Execute();
        return new ExecutionResult<CreatePostingResponse, CreatePostingVerification>(result);
    }
}
