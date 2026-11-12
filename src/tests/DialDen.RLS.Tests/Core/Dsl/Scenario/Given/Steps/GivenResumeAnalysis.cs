using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public sealed class GivenResumeAnalysis(GivenStage stage) : BaseGiven(stage)
{
    private ExtractedContactInformation _contacts = new(null, null, null, null, null);
    private bool _holdResponse;
    private bool _fails;
    public GivenResumeAnalysis IsUnavailable()
    {
        _fails = true;
        return this;
    }

    public GivenResumeAnalysis HasNotCompletedAnalysis()
    {
        _holdResponse = true;
        return this;
    }

    public GivenResumeAnalysis FindsName(string name)
    {
        _contacts = _contacts with { Name = name };
        return this;
    }

    public GivenResumeAnalysis FindsEmail(string email)
    {
        _contacts = _contacts with { Email = email };
        return this;
    }

    public GivenResumeAnalysis FindsPhoneNumber(string phoneNumber)
    {
        _contacts = _contacts with { PhoneNumber = phoneNumber };
        return this;
    }

    public GivenResumeAnalysis FindsPostalAddress(string postalAddress)
    {
        _contacts = _contacts with { PostalAddress = postalAddress };
        return this;
    }

    public GivenResumeAnalysis FindsLinkedInUrl(string linkedInUrl)
    {
        _contacts = _contacts with { LinkedInUrl = linkedInUrl };
        return this;
    }

    internal override Task Execute(UseCaseDsl app) =>
        _fails ? app.ExternalSystems.ResumeAnalysis.FailsAnalysis()
            : app.ExternalSystems.ResumeAnalysis.FindsContactInformation(_contacts, _holdResponse);
}
