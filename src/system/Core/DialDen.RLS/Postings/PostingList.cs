namespace DialDen.RLS.Postings;

public sealed record PostingList(IReadOnlyList<Posting> Postings, long TotalPostings);
