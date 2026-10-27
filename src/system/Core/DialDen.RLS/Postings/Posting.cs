using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public sealed record Posting(int Id, string Title, PostingStatus Status);
