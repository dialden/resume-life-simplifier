using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenApplyToPosting : BaseWhen<VoidValue, ApplicationSubmissionVerification>
{
    private string _postingIdAlias = ScenarioDefaults.PostingId;
    private string _name = "Alex Example";
    private string _email = "alex@example.com";
    private byte[]? _resume;
    private string? _referralJobSiteId;

    internal WhenApplyToPosting(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
        : base(app, scenario, setup)
    {
    }

    public WhenApplyToPosting WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _postingIdAlias = idAlias;
        return this;
    }

    public WhenApplyToPosting WithName(string name)
    {
        _name = name;
        return this;
    }

    public WhenApplyToPosting WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public WhenApplyToPosting WithResume()
    {
        _resume = SampleResume.Create();
        return this;
    }

    public WhenApplyToPosting WithUnreadableResume()
    {
        _resume = "%PDF-1.4\nunreadable document\n%%EOF"u8.ToArray();
        return this;
    }

    public WhenApplyToPosting WithResumePages(int pages)
    {
        _resume = SampleResume.CreateWordDocument(pages);
        return this;
    }

    public WhenApplyToPosting ReferredBy(string jobSiteId)
    {
        _referralJobSiteId = jobSiteId;
        return this;
    }

    public WhenApplyToPosting WithWordResume()
    {
        _resume = SampleResume.CreateWordDocument();
        return this;
    }

    public WhenApplyToPosting WithInvalidWordResume()
    {
        _resume = "PK\x03\x04not a Word document"u8.ToArray();
        return this;
    }

    public WhenApplyToPosting WithResumeOfSize(int size)
    {
        var sample = SampleResume.Create();
        if (size < sample.Length) throw new ArgumentOutOfRangeException(nameof(size));
        _resume = new byte[size];
        sample.CopyTo(_resume, 0);
        return this;
    }

    protected override async Task<ExecutionResult<VoidValue, ApplicationSubmissionVerification>> Execute(UseCaseDsl app)
    {
        var result = await app.RLS.ApplyToPosting()
            .WithId(_postingIdAlias)
            .WithApplicant(_name, _email)
            .ReferredBy(_referralJobSiteId)
            .WithResume(_resume ?? throw new InvalidOperationException("A resume must be supplied."))
            .Execute();
        return new ExecutionResult<VoidValue, ApplicationSubmissionVerification>(result);
    }
}
