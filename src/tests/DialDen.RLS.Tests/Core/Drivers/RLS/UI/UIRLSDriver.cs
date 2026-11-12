using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.RLS.UI;

public class UIRLSDriver : IRLSDriver
{
    private readonly UIClient _uiClient;
    private PostingsPage? _postingsPage;

    public UIRLSDriver(UIClient uiClient)
    {
        _uiClient = uiClient;
    }

    public async Task<Result<PublicListingResponse, SystemError>> ViewPublicListing(int id, Uri? address = null)
    {
        var target = address ?? new Uri(_uiClient.PublicWebsiteBaseUrl, $"jobs/{id}");
        var publicPage = await _uiClient.OpenPublicListing(target);
        return await publicPage.ReadListing();
    }

    public async Task<Result<VoidValue, SystemError>> ApplyToPosting(
        int postingId, string name, string email, byte[] resume, string? referralJobSiteId)
    {
        var referral = referralJobSiteId is null ? "" : $"?referralJobSiteId={Uri.EscapeDataString(referralJobSiteId)}";
        var page = await _uiClient.OpenPublicListing(new Uri(_uiClient.PublicWebsiteBaseUrl, $"jobs/{postingId}{referral}"));
        return await page.Apply(name, email, resume);
    }

    public async Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ListApplications(int postingId)
    {
        var page = await _uiClient.OpenPosting(postingId);
        return await page.ReadApplications();
    }

    public Task<Result<VoidValue, SystemError>> InterruptLiveUpdates() =>
        _uiClient.InterruptLiveUpdates();

    public Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ObserveViewedApplications(
        int postingId, Func<IReadOnlyList<ApplicationResponse>, bool> isExpected, TimeSpan timeout)
    {
        var page = _uiClient.DisplayedApplicantsPage(postingId);
        return page.IsSuccess
            ? page.Value.ObserveApplications(isExpected, timeout)
            : Task.FromResult(Result<IReadOnlyList<ApplicationResponse>, SystemError>.Failure(page.Error));
    }

    public async Task<Result<byte[], SystemError>> DownloadResume(int postingId, int applicationId)
    {
        var page = await _uiClient.OpenPosting(postingId);
        return await page.DownloadResume(applicationId);
    }

    public async Task<Result<VoidValue, SystemError>> GoToRLS()
    {
        var homePage = await _uiClient.OpenHomePage();
        var error = await homePage.GetError();
        return error == null
            ? Result.Success<SystemError>()
            : Result.Failure<SystemError>(SystemError.Of($"The RLS UI displayed an error: {error}"));
    }

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title, string description)
    {
        var homePage = await _uiClient.OpenHomePage();
        return await homePage.CreatePosting(title, description);
    }

    public async Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id, IReadOnlyList<string> jobSiteIds)
    {
        var homePage = await _uiClient.OpenHomePage();
        var postingsPage = await homePage.OpenPostings();
        if (postingsPage.IsFailure)
        {
            return Result<PublishPostingResponse, SystemError>.Failure(postingsPage.Error);
        }

        return await postingsPage.Value.PublishPosting(id, jobSiteIds);
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        var homePage = await _uiClient.OpenHomePage();
        return await homePage.ViewPosting(id);
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ObserveViewedPosting(
        int id, Func<ViewPostingResponse, bool> isExpected, TimeSpan timeout)
    {
        var postingPage = _uiClient.DisplayedPostingPage(id);
        if (postingPage.IsFailure)
        {
            var list = _uiClient.DisplayedPostingsPage();
            if (list.IsFailure)
            {
                return Result<ViewPostingResponse, SystemError>.Failure(list.Error);
            }
            var observed = await list.Value.ObservePosting(id, isExpected, timeout);
            var connection = _uiClient.CheckSharedLiveUpdates();
            return observed.IsFailure || connection.IsSuccess
                ? observed
                : Result<ViewPostingResponse, SystemError>.Failure(connection.Error);
        }
        var result = await postingPage.Value.ObservePosting(isExpected, timeout);
        var sharedConnection = _uiClient.CheckSharedLiveUpdates();
        return result.IsFailure || sharedConnection.IsSuccess
            ? result
            : Result<ViewPostingResponse, SystemError>.Failure(sharedConnection.Error);
    }

    public async Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber)
    {
        if (_postingsPage is null)
        {
            var homePage = await _uiClient.OpenHomePage();
            var result = await homePage.OpenPostings();
            if (result.IsFailure)
            {
                return Result<PostingPageResponse, SystemError>.Failure(result.Error);
            }
            _postingsPage = result.Value;
        }
        return await _postingsPage.ReadPage(pageNumber);
    }
}
