namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public sealed class GivenEducationReview(GivenStage stage) : BaseGiven(stage)
{
    private bool? _verdict;

    public GivenEducationReview MarkMet() { _verdict = true; return this; }

    internal override async Task Execute(UseCaseDsl app)
    {
        var result = await app.RLS.ReviewEducation().WithVerdict(_verdict).WithNote("").Execute();
        result.ShouldSucceed();
    }
}
