namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public class GivenJobSite : BaseGiven
{
    private readonly string _jobSiteId;
    private readonly List<Func<UseCaseDsl, Task>> _actions = [];

    internal GivenJobSite(GivenStage givenStage, string jobSiteId) : base(givenStage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobSiteId);
        _jobSiteId = jobSiteId;
    }

    public GivenJobSite AcceptsPostings()
    {
        _actions.Add(app =>
        {
            app.ExternalSystems.JobSites.AcceptPostings(_jobSiteId);
            return Task.CompletedTask;
        });
        return this;
    }

    public GivenJobSite RespondsSlowly()
    {
        _actions.Add(app =>
        {
            app.ExternalSystems.JobSites.RespondSlowly(_jobSiteId);
            return Task.CompletedTask;
        });
        return this;
    }

    public GivenJobSite RejectsPostings()
    {
        _actions.Add(app =>
        {
            app.ExternalSystems.JobSites.RejectPostings(_jobSiteId);
            return Task.CompletedTask;
        });
        return this;
    }

    internal override async Task Execute(UseCaseDsl app)
    {
        foreach (var action in _actions)
        {
            await action(app);
        }
    }
}
