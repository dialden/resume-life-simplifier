namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public class GivenJobSite : BaseGiven
{
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(30);

    private readonly GivenStage _givenStage;
    private readonly string _jobSiteId;
    private readonly List<Func<UseCaseDsl, Task>> _actions = [];

    internal GivenJobSite(GivenStage givenStage, string jobSiteId) : base(givenStage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobSiteId);
        _givenStage = givenStage;
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

    public GivenJobSite RejectsPostings()
    {
        _actions.Add(app =>
        {
            app.ExternalSystems.JobSites.RejectPostings(_jobSiteId);
            return Task.CompletedTask;
        });
        return this;
    }

    /// <summary>The job site has answered every posting the scenario published to it.</summary>
    public GivenJobSite HasResponded()
    {
        _actions.Add(async app =>
        {
            var postings = _givenStage.PostingsPublishedTo(_jobSiteId);
            if (postings.Count == 0)
            {
                throw new InvalidOperationException($"No posting was published to job site '{_jobSiteId}' before it could respond.");
            }
            foreach (var posting in postings)
            {
                await app.RLS.ViewPosting().WithId(posting).UntilJobSiteResponded(_jobSiteId, ResponseTimeout);
            }
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
