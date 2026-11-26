using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Given.Steps;

public sealed class GivenResumeAnalysis(GivenStage stage) : BaseGiven(stage)
{
    private ExtractedContactInformation _contacts = new(null, null, null, null, null);
    private List<ExtractedEducation>? _education;
    private bool _holdResponse;
    private bool _fails;
    private bool _invalidEducation;
    private (bool? Met, double Confidence, string? Evidence)? _assessment;
    private bool _comparisonFails;
    private bool _holdComparison;

    public GivenResumeAnalysis HasNotCompletedEducationComparison()
    {
        _holdComparison = true;
        return this;
    }

    public GivenResumeAnalysis EducationComparisonIsUnavailable()
    {
        _comparisonFails = true;
        return this;
    }

    public GivenResumeAnalysis AssessesEducation(bool? met, double confidence, string? evidence)
    {
        _assessment = (met, confidence, evidence);
        return this;
    }
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

    public GivenResumeAnalysis FindsEducation(
        string? institution = null, string? qualification = null,
        string? startDate = null, string? endDate = null)
    {
        _education ??= [];
        _education.Add(new(institution, qualification, startDate, endDate));
        return this;
    }

    public GivenResumeAnalysis FindsNoEducation()
    {
        _education = [];
        return this;
    }

    public GivenResumeAnalysis ReturnsInvalidEducation()
    {
        _invalidEducation = true;
        return this;
    }

    internal override async Task Execute(UseCaseDsl app)
    {
        await (_fails ? app.ExternalSystems.ResumeAnalysis.FailsAnalysis()
            : _invalidEducation ? app.ExternalSystems.ResumeAnalysis.ReturnsInvalidEducation(_contacts)
            : _education is null
                ? app.ExternalSystems.ResumeAnalysis.FindsContactInformation(_contacts, _holdResponse)
                : app.ExternalSystems.ResumeAnalysis.FindsEducation(_contacts, _education, _holdResponse));
        if (_assessment is { } assessment)
            await app.ExternalSystems.ResumeAnalysis.AssessesEducation(assessment.Met, assessment.Confidence, assessment.Evidence);
        if (_comparisonFails)
            await app.ExternalSystems.ResumeAnalysis.FailsEducationComparison();
        if (_holdComparison)
            await app.ExternalSystems.ResumeAnalysis.HoldEducationComparison();
    }
}
