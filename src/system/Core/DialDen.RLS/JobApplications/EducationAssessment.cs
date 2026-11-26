using DialDen.RLS.Abstractions.Applications;

namespace DialDen.RLS.JobApplications;

public sealed record EducationAssessment
{
    private EducationAssessment() { }

    public EducationAssessment(bool? originalMet, double confidence, string? evidence)
    {
        if (!double.IsFinite(confidence) || confidence is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence));
        if (evidence?.Length > 4000)
            throw new ArgumentOutOfRangeException(nameof(evidence));
        OriginalMet = originalMet;
        Confidence = confidence;
        Evidence = originalMet is false ? null : evidence;
    }

    public bool? OriginalMet { get; private init; }
    public double Confidence { get; private init; }
    public string? Evidence { get; private init; }
    public bool? OverrideMet { get; private init; }
    public string? Note { get; private init; }

    public ApplicationReviewGroup ReviewGroup => OverrideMet switch
    {
        true => ApplicationReviewGroup.Qualified,
        false => ApplicationReviewGroup.Disqualified,
        null when OriginalMet is false => ApplicationReviewGroup.Disqualified,
        null when OriginalMet is null || Confidence < 0.85 => ApplicationReviewGroup.Verify,
        null => ApplicationReviewGroup.Qualified
    };

    public EducationAssessment Review(bool? overrideMet, string note)
    {
        ArgumentNullException.ThrowIfNull(note);
        if (note.Length > 4000)
            throw new ArgumentOutOfRangeException(nameof(note));
        return this with { OverrideMet = overrideMet, Note = note };
    }

    public Abstractions.Applications.EducationAssessment ToResponse() =>
        new(OriginalMet, Confidence, Evidence, OverrideMet, Note ?? "");
}
