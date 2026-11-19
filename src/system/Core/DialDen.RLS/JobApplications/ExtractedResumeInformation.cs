using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public sealed record ExtractedResumeInformation(
    ExtractedContactInformation Contacts, IReadOnlyList<ExtractedEducation> Education);
