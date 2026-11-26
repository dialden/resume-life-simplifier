namespace DialDen.RLS.Abstractions.Applications;

public enum ApplicationStatus
{
    NotProcessed,
    AwaitingExtraction,
    ExtractionFailed,
    AwaitingComparison,
    ComparisonFailed,
    Ready
}
