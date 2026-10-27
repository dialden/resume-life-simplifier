using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenListPostings : BaseWhen<ListPostingsResponse, ListPostingsVerification>
{
    private int? _pageNumber = 1;

    internal WhenListPostings(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
        : base(app, scenario, setup) { }

    public WhenListPostings OnPage(int pageNumber)
    {
        _pageNumber = pageNumber;
        return this;
    }

    public WhenListPostings AcrossAllPages()
    {
        _pageNumber = null;
        return this;
    }

    protected override async Task<ExecutionResult<ListPostingsResponse, ListPostingsVerification>> Execute(UseCaseDsl app)
    {
        var useCase = app.RLS.ListPostings();
        if (_pageNumber is { } pageNumber)
        {
            useCase.OnPage(pageNumber);
        }
        else
        {
            useCase.AcrossAllPages();
        }
        return new(await useCase.Execute());
    }
}
