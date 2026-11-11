using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public sealed class Application(
    int id, int postingId, string name, string email, string resumeFileName,
    byte[] resume, string source, string? referralJobSiteId, InformationExtractionStatus extractionStatus)
{
    public int Id { get; private set; } = id;
    public int PostingId { get; private set; } = postingId;
    public string Name { get; private set; } = name;
    public string Email { get; private set; } = email;
    public string ResumeFileName { get; private set; } = resumeFileName;
    public byte[] Resume { get; private set; } = resume;
    public string Source { get; private set; } = source;
    public string? ReferralJobSiteId { get; private set; } = referralJobSiteId;
    public InformationExtractionStatus ExtractionStatus { get; private set; } = extractionStatus;
    public string? ExtractedName { get; private set; }
    public string? ExtractedEmail { get; private set; }
    public string? ExtractedPhoneNumber { get; private set; }
    public string? ExtractedPostalAddress { get; private set; }
    public string? ExtractedLinkedInUrl { get; private set; }
    public ExtractedContactInformation? ExtractedContacts => ExtractionStatus == InformationExtractionStatus.Completed
        ? new ExtractedContactInformation(
            ExtractedName, ExtractedEmail, ExtractedPhoneNumber, ExtractedPostalAddress, ExtractedLinkedInUrl)
        : null;

    public void FailExtraction()
    {
        if (ExtractionStatus != InformationExtractionStatus.Pending)
            throw new InvalidOperationException($"Information extraction for application {Id} is not pending.");

        ExtractionStatus = InformationExtractionStatus.Failed;
        ExtractedName = null;
        ExtractedEmail = null;
        ExtractedPhoneNumber = null;
        ExtractedPostalAddress = null;
        ExtractedLinkedInUrl = null;
    }

    public void CompleteExtraction(ExtractedContactInformation contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        if (ExtractionStatus != InformationExtractionStatus.Pending)
            throw new InvalidOperationException($"Information extraction for application {Id} is not pending.");

        ExtractionStatus = InformationExtractionStatus.Completed;
        ExtractedName = contacts.Name;
        ExtractedEmail = contacts.Email;
        ExtractedPhoneNumber = contacts.PhoneNumber;
        ExtractedPostalAddress = contacts.PostalAddress;
        ExtractedLinkedInUrl = contacts.LinkedInUrl;
    }
}
