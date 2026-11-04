using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.RLS;

public interface IRLSDriver
{
    Task<Result<VoidValue, SystemError>> GoToRLS();
    Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title, string description);
    Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id, IReadOnlyList<string> jobSiteIds);
    Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id);

    /// <summary>
    /// Keeps observing the posting being viewed, without viewing it again, until <paramref name="isExpected"/>
    /// holds or <paramref name="timeout"/> elapses, and returns the latest observation.
    /// </summary>
    Task<Result<ViewPostingResponse, SystemError>> ObserveViewedPosting(
        int id, Func<ViewPostingResponse, bool> isExpected, TimeSpan timeout);
    Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber);
}
