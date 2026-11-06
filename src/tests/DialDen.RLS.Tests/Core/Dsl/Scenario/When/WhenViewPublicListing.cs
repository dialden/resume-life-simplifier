using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenViewPublicListing : BaseWhen<PublicListingResponse, PublicListingVerification>
{
    private string? _idAlias = ScenarioDefaults.PostingId;

    internal WhenViewPublicListing(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
        : base(app, scenario, setup)
    {
    }

    public WhenViewPublicListing WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    public WhenViewPublicListing ForUnknownPosting()
    {
        _idAlias = null;
        return this;
    }

    protected override async Task<ExecutionResult<PublicListingResponse, PublicListingVerification>> Execute(UseCaseDsl app)
    {
        var view = app.RLS.ViewPublicListing();
        if (_idAlias is null)
        {
            view.WithId(0);
        }
        else
        {
            view.WithId(_idAlias);
        }
        return new ExecutionResult<PublicListingResponse, PublicListingVerification>(await view.Execute());
    }
}
