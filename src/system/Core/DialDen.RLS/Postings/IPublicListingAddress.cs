using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public interface IPublicListingAddress
{
    Uri ForPosting(int postingId);
}
