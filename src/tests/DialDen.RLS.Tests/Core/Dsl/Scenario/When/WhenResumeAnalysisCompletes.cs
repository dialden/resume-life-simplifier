using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenResumeAnalysisCompletes(
    UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
    : BaseWhen<VoidValue, ApplicationReviewVerification>(app, scenario, setup)
{
    private ExtractedContactInformation _contacts = new(null, null, null, null, null);
    private bool _fails;
    private bool _interruptConnection;

    public WhenResumeAnalysisCompletes AfterConnectionInterruption()
    {
        _interruptConnection = true;
        return this;
    }

    public WhenResumeAnalysisCompletes WithFailure()
    {
        _fails = true;
        return this;
    }

    public WhenResumeAnalysisCompletes FindsEmail(string? email)
    {
        _contacts = _contacts with { Email = email };
        return this;
    }

    protected override async Task<ExecutionResult<VoidValue, ApplicationReviewVerification>> Execute(UseCaseDsl app)
    {
        var analysis = app.RLS.CompleteResumeAnalysis().Finding(_contacts);
        if (_interruptConnection)
            analysis.AfterConnectionInterruption();
        if (_fails)
            analysis.WithFailure();
        return new(await analysis.Execute());
    }
}
