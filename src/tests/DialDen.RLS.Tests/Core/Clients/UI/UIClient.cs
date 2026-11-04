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
            var browserType = _playwright[PlaywrightSettingsProvider.BrowserName];
            _browser = await browserType.LaunchAsync();
            _page = await _browser.NewPageAsync(new BrowserNewPageOptions { BaseURL = _configuration.UIUrl });
            _page.Request += (_, request) =>
            {
                if (new Uri(request.Url).AbsolutePath == "/notifications/stream")
                {
                    Interlocked.Increment(ref _notificationStreamRequests);
                }
            };
            await _page.GotoAsync(string.Empty);
            return new HomePage(_page);
        }

        return await new HomePage(_page).GoToHomePage();
    }

    public Result<PostingPage, SystemError> DisplayedPostingPage(int id)
    {
        var path = _page is null ? null : new Uri(_page.Url).AbsolutePath;
        return path == $"/postings/{id}"
            ? Result<PostingPage, SystemError>.Success(new PostingPage(_page!))
            : Result<PostingPage, SystemError>.Failure(SystemError.Of($"Posting {id} is not displayed."));
    }

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
