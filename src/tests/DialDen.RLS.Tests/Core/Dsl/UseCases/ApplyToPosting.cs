using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ApplyToPosting(IRLSDriver driver, UseCaseContext context)
    : RLSUseCase<VoidValue, ApplicationSubmissionVerification>(driver, context)
{
    private string _idAlias = Scenario.ScenarioDefaults.PostingId;
    private string _name = "";
    private string _email = "";
    private byte[] _resume = [];
    private string? _referralJobSiteId;

    public ApplyToPosting WithId(string idAlias)
    {
        _idAlias = idAlias;
        return this;
    }

    public ApplyToPosting WithApplicant(string name, string email)
    {
        _name = name;
        _email = email;
        return this;
    }

    public ApplyToPosting WithResume(byte[] resume)
    {
        _resume = resume;
        return this;
    }

    public ApplyToPosting ReferredBy(string? jobSiteId)
    {
        _referralJobSiteId = jobSiteId;
        return this;
    }

    public override async Task<RLSUseCaseResult<VoidValue, ApplicationSubmissionVerification>> Execute()
    {
        if (!int.TryParse(Context.GetResultValue(_idAlias), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var postingId))
        {
            throw new InvalidOperationException($"Posting ID alias '{_idAlias}' did not resolve to an integer ID.");
        }

        var result = await Driver.ApplyToPosting(postingId, _name, _email, _resume, _referralJobSiteId);
        return new RLSUseCaseResult<VoidValue, ApplicationSubmissionVerification>(
            result, Context,
            (response, context) => new ApplicationSubmissionVerification(
                response, context, Driver, postingId, _email, _resume));
    }
}
