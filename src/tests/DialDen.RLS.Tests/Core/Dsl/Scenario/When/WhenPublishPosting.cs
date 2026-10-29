using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenPublishPosting : BaseWhen<PublishPostingResponse, PublishPostingVerification>
{
    private string _idAlias = ScenarioDefaults.PostingId;
    private string[] _jobSiteIds = [];

    internal WhenPublishPosting(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
        : base(app, scenario, setup)
    {
    }

    public WhenPublishPosting WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    public WhenPublishPosting ToJobSites(params string[] jobSiteIds)
    {
        _jobSiteIds = jobSiteIds;
        return this;
    }

    protected override async Task<ExecutionResult<PublishPostingResponse, PublishPostingVerification>> Execute(UseCaseDsl app)
    {
        var result = await app.RLS.PublishPosting().WithId(_idAlias).ToJobSites(_jobSiteIds).Execute();
        return new ExecutionResult<PublishPostingResponse, PublishPostingVerification>(result);
    }
}
