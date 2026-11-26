using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ReviewEducation(IRLSDriver driver, UseCaseContext context)
    : RLSUseCase<VoidValue, EducationReviewVerification>(driver, context)
{
    private bool? _verdict;
    private string _note = "";

    public ReviewEducation WithVerdict(bool? verdict) { _verdict = verdict; return this; }
    public ReviewEducation WithNote(string note) { _note = note; return this; }

    public override async Task<RLSUseCaseResult<VoidValue, EducationReviewVerification>> Execute()
    {
        var postingId = int.Parse(Context.GetResultValue(Scenario.ScenarioDefaults.PostingId)
            ?? throw new InvalidOperationException("The posting has not been created."), CultureInfo.InvariantCulture);
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        DialDen.RLS.Tests.Core.Drivers.RLS.Dtos.ApplicationResponse application;
        do
        {
            var applications = await Driver.ListApplications(postingId);
            if (applications.IsFailure)
                throw new InvalidOperationException($"Could not review applications: {applications.Error}");
            application = applications.Value.Single();
            if (application.ReviewGroup is "Qualified" or "Verify" or "Disqualified")
                break;
            await Task.Delay(250);
        } while (elapsed.Elapsed < TimeSpan.FromSeconds(30));
        if (application.ReviewGroup is not ("Qualified" or "Verify" or "Disqualified"))
            throw new InvalidOperationException("The application was not assessed.");
        var result = await Driver.ReviewEducation(postingId, application.Id, _verdict, _note, application.ReviewVersion);
        return new(result, Context, (response, ctx) =>
            new EducationReviewVerification(response, ctx, Driver, postingId, application.Id));
    }
}
