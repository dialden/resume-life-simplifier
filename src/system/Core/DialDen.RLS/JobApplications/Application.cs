using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public sealed class Application(
    int id, int postingId, string name, string email, string resumeFileName,
    byte[] resume, string source, string? referralJobSiteId, ApplicationStatus status)
{
    private readonly List<EducationHistoryEntry> _educationHistory = [];

    public int Id { get; private set; } = id;
    public int PostingId { get; private set; } = postingId;
    public string Name { get; private set; } = name;
    public string Email { get; private set; } = email;
    public string ResumeFileName { get; private set; } = resumeFileName;
    public byte[] Resume { get; private set; } = resume;
    public string Source { get; private set; } = source;
    public string? ReferralJobSiteId { get; private set; } = referralJobSiteId;
    public ApplicationStatus Status { get; private set; } = status;
    public int ReviewVersion { get; private set; }
    public EducationAssessment? EducationAssessment { get; private set; }
    public string? ExtractedName { get; private set; }
    public string? ExtractedEmail { get; private set; }
    public string? ExtractedPhoneNumber { get; private set; }
    public string? ExtractedPostalAddress { get; private set; }
    public string? ExtractedLinkedInUrl { get; private set; }
    public IReadOnlyList<EducationHistoryEntry> EducationHistory => _educationHistory;
    public bool HasExtractedInformation => Status is ApplicationStatus.AwaitingComparison
        or ApplicationStatus.ComparisonFailed or ApplicationStatus.Ready;
    public ExtractedContactInformation? ExtractedContacts => HasExtractedInformation
        ? new ExtractedContactInformation(
            ExtractedName, ExtractedEmail, ExtractedPhoneNumber, ExtractedPostalAddress, ExtractedLinkedInUrl)
        : null;
    public IReadOnlyList<ExtractedEducation>? ExtractedEducation =>
        HasExtractedInformation
            ? _educationHistory.OrderBy(entry => entry.Position).Select(entry => entry.ToExtractedEducation()).ToArray()
            : null;

    public ApplicationReviewGroup ReviewGroup => GroupFor(Status, EducationAssessment);

    public static ApplicationReviewGroup GroupFor(ApplicationStatus status, EducationAssessment? assessment) => status switch
    {
        ApplicationStatus.NotProcessed or ApplicationStatus.AwaitingExtraction => ApplicationReviewGroup.AwaitingExtraction,
        ApplicationStatus.ExtractionFailed => ApplicationReviewGroup.ExtractionFailed,
        ApplicationStatus.AwaitingComparison => ApplicationReviewGroup.AwaitingComparison,
        ApplicationStatus.ComparisonFailed => ApplicationReviewGroup.ComparisonFailed,
        ApplicationStatus.Ready => assessment?.ReviewGroup ?? ApplicationReviewGroup.Qualified,
        _ => throw new InvalidOperationException($"Unknown application status: {status}.")
    };

    public void FailExtraction()
    {
        if (Status != ApplicationStatus.AwaitingExtraction)
            throw new InvalidOperationException($"Information extraction for application {Id} is not pending.");

        Status = ApplicationStatus.ExtractionFailed;
        ExtractedName = null;
        ExtractedEmail = null;
        ExtractedPhoneNumber = null;
        ExtractedPostalAddress = null;
        ExtractedLinkedInUrl = null;
    }

    public void CompleteExtraction(ExtractedResumeInformation information, bool requiresComparison)
    {
        ArgumentNullException.ThrowIfNull(information);
        ArgumentNullException.ThrowIfNull(information.Contacts);
        ArgumentNullException.ThrowIfNull(information.Education);
        if (Status != ApplicationStatus.AwaitingExtraction)
            throw new InvalidOperationException($"Information extraction for application {Id} is not pending.");

        var contacts = information.Contacts;
        _educationHistory.AddRange(information.Education.Select((entry, position) =>
            new EducationHistoryEntry(position, entry.Institution, entry.Qualification,
                entry.StartDate, entry.EndDate)));
        Status = requiresComparison ? ApplicationStatus.AwaitingComparison : ApplicationStatus.Ready;
        ExtractedName = contacts.Name;
        ExtractedEmail = contacts.Email;
        ExtractedPhoneNumber = contacts.PhoneNumber;
        ExtractedPostalAddress = contacts.PostalAddress;
        ExtractedLinkedInUrl = contacts.LinkedInUrl;
    }

    public void CompleteComparison(bool? met, double confidence, string? evidence)
    {
        if (Status != ApplicationStatus.AwaitingComparison)
            throw new InvalidOperationException($"Comparison for application {Id} is not pending.");
        EducationAssessment = new EducationAssessment(met, confidence, evidence);
        Status = ApplicationStatus.Ready;
    }

    public void FailComparison()
    {
        if (Status != ApplicationStatus.AwaitingComparison)
            throw new InvalidOperationException($"Comparison for application {Id} is not pending.");
        Status = ApplicationStatus.ComparisonFailed;
    }

    public void ReviewEducation(bool? overrideMet, string note)
    {
        if (Status != ApplicationStatus.Ready || EducationAssessment is null)
            throw new InvalidOperationException($"Application {Id} has no education assessment to review.");
        EducationAssessment = EducationAssessment.Review(overrideMet, note);
        ReviewVersion = checked(ReviewVersion + 1);
    }
}
