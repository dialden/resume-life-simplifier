using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.RLS;

public interface IRLSDriver
{
    Task<Result<VoidValue, SystemError>> GoToRLS();
    Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title, string description);
    Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id, IReadOnlyList<string> jobSiteIds);
    Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id);
    Task<Result<PublicListingResponse, SystemError>> ViewPublicListing(int id, Uri? address = null);
    Task<Result<VoidValue, SystemError>> ApplyToPosting(
        int postingId, string name, string email, byte[] resume, string? referralJobSiteId);
    Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ListApplications(int postingId);
    Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ObserveViewedApplications(
        int postingId, Func<IReadOnlyList<ApplicationResponse>, bool> isExpected, TimeSpan timeout);
    Task<Result<byte[], SystemError>> DownloadResume(int postingId, int applicationId);

    /// <summary>
    /// Keeps observing the posting being viewed, without viewing it again, until <paramref name="isExpected"/>
    /// holds or <paramref name="timeout"/> elapses, and returns the latest observation.
    /// </summary>
    Task<Result<ViewPostingResponse, SystemError>> ObserveViewedPosting(
        int id, Func<ViewPostingResponse, bool> isExpected, TimeSpan timeout);
    Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber);
}
