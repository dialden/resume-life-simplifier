namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public class GivenPosting : BaseGiven
{
    private string _idAlias = ScenarioDefaults.PostingId;
    private string _title = "Software Engineer";
    private string _description = "Build and maintain software for our customers.";

    public GivenPosting WithDescription(string description)
    {
        _description = description;
        return this;
    }
    private int _count = 1;
    private string[]? _publishedJobSites;
    private bool _completedPublication;

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
        for (var index = 1; index <= _count; index++)
        {
            var result = await app.RLS.CreatePosting().WithTitle(_title).WithDescription(_description).Execute();
            var alias = _count == 1 ? _idAlias : $"{_idAlias}-{index}";
            var postingId = result.ShouldSucceed().SaveIdAs(alias).Id
                ?? throw new InvalidOperationException("The created posting did not include an ID.");
            if (_publishedJobSites is not null)
            {
                await app.ExternalSystems.JobSites.ConfigureForPosting(postingId, _publishedJobSites);
                var published = await app.RLS.PublishPosting().WithId(alias).ToJobSites(_publishedJobSites).Execute();
                published.ShouldSucceed();
                if (_completedPublication)
                {
                    foreach (var jobSiteId in _publishedJobSites)
                    {
                        await app.RLS.ViewPosting().WithId(postingId).UntilJobSiteResponded(
                            jobSiteId, TimeSpan.FromSeconds(30), "Succeeded");
                    }
                }
            }
        }
    }

    public GivenPosting PublishedTo(params string[] jobSiteIds)
    {
        _publishedJobSites = jobSiteIds;
        _completedPublication = false;
        return this;
    }

    public GivenPosting PublishedOnJobSite(string jobSiteId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobSiteId);
        _publishedJobSites = [jobSiteId];
        _completedPublication = true;
        return this;
    }

    public GivenPosting WithCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        _count = count;
        return this;
    }
}
