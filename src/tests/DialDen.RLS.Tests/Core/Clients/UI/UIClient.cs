using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using DialDen.RLS.Tests.Fixtures;
using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public class UIClient : IDisposable
{
    private readonly IPlaywright _playwright;
    private readonly RLSConfiguration _configuration;
    private IBrowser? _browser;
    private IPage? _page;
    private int _notificationStreamRequests;

    public UIClient(RLSConfiguration configuration, IPlaywright playwright)
    {
        _configuration = configuration;
        _playwright = playwright;
    }

    public async Task<HomePage> OpenHomePage()
    {
        if (_page == null)
        {
            var page = await CreatePage();
            await page.GotoAsync(string.Empty);
            return new HomePage(page);
        }

        return await new HomePage(_page).GoToHomePage();
    }

    public Uri PublicWebsiteBaseUrl => new(_configuration.UIUrl);

    public async Task<PublicListingPage> OpenPublicListing(Uri address)
    {
        var page = _page ?? await CreatePage();
        await page.GotoAsync(new Uri(new Uri(_configuration.UIUrl), address.PathAndQuery).AbsoluteUri);
        return new PublicListingPage(page);
    }

    public async Task<PostingPage> OpenPosting(int id)
    {
        var page = _page ?? await CreatePage();
        await page.GotoAsync($"/postings/{id}");
        return new PostingPage(page);
    }

    private async Task<IPage> CreatePage()
    {
        var browserType = _playwright[PlaywrightSettingsProvider.BrowserName];
        _browser = await browserType.LaunchAsync();
        var page = await _browser.NewPageAsync(new BrowserNewPageOptions { BaseURL = _configuration.UIUrl });
        page.Request += (_, request) =>
        {
            if (new Uri(request.Url).AbsolutePath == "/notifications/stream")
            {
                Interlocked.Increment(ref _notificationStreamRequests);
            }
        };
        _page = page;
        return page;
    }

    public Result<PostingPage, SystemError> DisplayedPostingPage(int id)
    {
        var path = _page is null ? null : new Uri(_page.Url).AbsolutePath;
        return path == $"/postings/{id}"
            ? Result<PostingPage, SystemError>.Success(new PostingPage(_page!))
            : Result<PostingPage, SystemError>.Failure(SystemError.Of($"Posting {id} is not displayed."));
    }

    public Result<ApplicantsPage, SystemError> DisplayedApplicantsPage(int id) =>
        _page is not null && new Uri(_page.Url).AbsolutePath == $"/postings/{id}/applicants"
            ? Result<ApplicantsPage, SystemError>.Success(new ApplicantsPage(_page))
            : Result<ApplicantsPage, SystemError>.Failure(SystemError.Of(
                $"Applicants for posting {id} are not displayed."));

    public Result<VoidValue, SystemError> CheckSharedLiveUpdates()
    {
        var requests = Volatile.Read(ref _notificationStreamRequests);
        return requests == 1
            ? Result.Success<SystemError>()
            : Result.Failure<SystemError>(SystemError.Of(
                $"Expected one shared notification stream across navigation, but opened {requests}."));
    }

    public void Dispose()
    {
        _browser?.CloseAsync().GetAwaiter().GetResult();
    }

    public Result<PostingsPage, SystemError> DisplayedPostingsPage()
    {
        if (_page is not null && new Uri(_page.Url).AbsolutePath == "/postings")
        {
            return Result<PostingsPage, SystemError>.Success(new PostingsPage(_page));
        }
        return Result<PostingsPage, SystemError>.Failure(SystemError.Of("The postings list is not displayed."));
    }
}
