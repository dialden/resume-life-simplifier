using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Dsl.External.ResumeAnalysis;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class CompleteResumeAnalysis(
    IRLSDriver driver, UseCaseContext context, ResumeAnalysisDsl resumeAnalysis)
    : RLSUseCase<VoidValue, ApplicationReviewVerification>(driver, context)
{
    private ExtractedContactInformation _contacts = new(null, null, null, null, null);
    private bool _fails;
    private bool _interruptConnection;

    public CompleteResumeAnalysis AfterConnectionInterruption()
    {
        _interruptConnection = true;
        return this;
    }

    public CompleteResumeAnalysis WithFailure()
    {
        _fails = true;
        return this;
    }

    public CompleteResumeAnalysis Finding(ExtractedContactInformation contacts)
    {
        _contacts = contacts;
        return this;
    }

    public override async Task<RLSUseCaseResult<VoidValue, ApplicationReviewVerification>> Execute()
    {
        var postingId = int.Parse(Context.GetResultValue(Scenario.ScenarioDefaults.PostingId)
            ?? throw new InvalidOperationException("The posting has not been created."), CultureInfo.InvariantCulture);
        var applications = await Driver.ListApplications(postingId);
        if (applications.IsFailure)
            throw new InvalidOperationException($"Could not review applications: {applications.Error}");
        var application = applications.Value.Single();
        if (application.ExtractionStatus != "Pending")
            throw new InvalidOperationException("Resume analysis must still be pending before completion.");
        if (_interruptConnection)
        {
            var interruption = await Driver.InterruptLiveUpdates();
            if (interruption.IsFailure)
                throw new InvalidOperationException($"Could not interrupt live updates: {interruption.Error}");
        }
        if (_fails)
            await resumeAnalysis.FailsAnalysis();
        else
            await resumeAnalysis.FindsContactInformation(_contacts, holdResponse: false);
        return new(
            Result.Success<SystemError>(), Context,
            (response, ctx) => new ApplicationReviewVerification(
                response, ctx, Driver, postingId, application.Id));
    }
}
