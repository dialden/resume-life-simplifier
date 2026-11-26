using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public interface IEducationComparison
{
    Task<EducationComparisonResult> Compare(
        string requirement, IReadOnlyList<ExtractedEducation> education, CancellationToken cancellationToken);
}
