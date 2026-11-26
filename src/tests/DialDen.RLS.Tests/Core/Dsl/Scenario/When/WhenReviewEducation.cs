using DialDen.RLS.Tests.Core.Dsl.UseCases;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.When;

public sealed class WhenReviewEducation(UseCaseDsl app, ScenarioDsl scenario, Func<Task> setup)
    : BaseWhen<VoidValue, EducationReviewVerification>(app, scenario, setup)
{
    private bool? _verdict;
    private string _note = "";

    public WhenReviewEducation MarkMet() { _verdict = true; return this; }
    public WhenReviewEducation RestoreOriginal() { _verdict = null; return this; }
    public WhenReviewEducation WithNote(string note) { _note = note; return this; }

    protected override async Task<ExecutionResult<VoidValue, EducationReviewVerification>> Execute(UseCaseDsl app) =>
        new(await app.RLS.ReviewEducation().WithVerdict(_verdict).WithNote(_note).Execute());
}
