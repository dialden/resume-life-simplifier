namespace DialDen.RLS.Abstractions.Postings;

public sealed class PostingAlreadyPublishedException()
    : Exception("This posting cannot be published again.");
