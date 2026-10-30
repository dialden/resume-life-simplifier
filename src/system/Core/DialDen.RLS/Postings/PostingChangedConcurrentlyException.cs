namespace DialDen.RLS.Postings;

public sealed class PostingChangedConcurrentlyException()
    : Exception("The posting was changed by someone else since it was found.");
