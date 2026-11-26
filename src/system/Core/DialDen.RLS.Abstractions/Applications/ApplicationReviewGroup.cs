namespace DialDen.RLS.Abstractions.Applications;

public enum ApplicationReviewGroup
{
    AwaitingExtraction,
    ExtractionFailed,
    AwaitingComparison,
    ComparisonFailed,
    Qualified,
    Verify,
    Disqualified
}
