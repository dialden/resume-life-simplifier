using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using DialDen.RLS.Tests.Fixtures;
using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public class UIClient : IAsyncDisposable
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

    /// <summary>
    /// Blocks Blazor startup so it cannot replace the loading screen before inspection.
    /// Verifies the logo image loads and decodes, then returns its path; does not check animation.
    /// </summary>
    public async Task<string?> ReadLoadingLogoPathBeforeBlazorStarts()
    {
        var page = await CreatePage();
        await page.RouteAsync("**/_framework/blazor.webassembly*", route => route.AbortAsync());
        await page.GotoAsync(string.Empty, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var logo = page.Locator("#app img");
        await logo.WaitForAsync();
        await logo.EvaluateAsync("img => img.decode()");
        return await logo.GetAttributeAsync("src");
    }

    public Task<string> ReadLoadingCompanyNameBeforeBlazorStarts()
    {
        var page = _page ?? throw new InvalidOperationException("The startup loading screen has not been opened.");
        return page.Locator("#app .loading-progress-text").InnerTextAsync();
    }

    public Task<string> ReadLoadingLogoAnimationNameBeforeBlazorStarts()
    {
        var page = _page ?? throw new InvalidOperationException("The startup loading screen has not been opened.");
        return page.Locator("#app img").EvaluateAsync<string>(
            "image => getComputedStyle(image).animationName");
    }

    public Task<string> ReadLoadingScreenBackgroundColorBeforeBlazorStarts()
    {
        var page = _page ?? throw new InvalidOperationException("The startup loading screen has not been opened.");
        return page.Locator("#app").EvaluateAsync<string>(
            "element => getComputedStyle(element).backgroundColor");
    }

    public Task<string> ReadLoadingLogoFilterBeforeBlazorStarts()
    {
        var page = _page ?? throw new InvalidOperationException("The startup loading screen has not been opened.");
        return page.Locator("#app img").EvaluateAsync<string>(
            "image => getComputedStyle(image).filter");
    }

    public Task<bool> IsLoadingBarVisibleBeforeBlazorStarts()
    {
        var page = _page ?? throw new InvalidOperationException("The startup loading screen has not been opened.");
        return page.GetByRole(AriaRole.Progressbar).IsVisibleAsync();
    }

    public Task<string> ReadLoadingBarAnimationNameBeforeBlazorStarts()
    {
        var page = _page ?? throw new InvalidOperationException("The startup loading screen has not been opened.");
        return page.Locator("#app .loading-bar-indicator").EvaluateAsync<string>(
            "indicator => getComputedStyle(indicator).animationName");
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
        await page.AddInitScriptAsync("""
            const originalFetch = window.fetch.bind(window);
            let notificationConnection;
            window.fetch = (input, options) => {
                const url = typeof input === "string" ? input : input.url;
                if (new URL(url, location.href).pathname !== "/notifications/stream")
                    return originalFetch(input, options);
                const controller = new AbortController();
                options?.signal?.addEventListener("abort", () => controller.abort(), { once: true });
                notificationConnection = controller;
                return originalFetch(input, { ...options, signal: controller.signal });
            };
            window.interruptTestNotificationConnection = () => {
                if (!notificationConnection || notificationConnection.signal.aborted)
                    return false;
                notificationConnection.abort();
                return true;
            };
            """);
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

    public async Task<Result<VoidValue, SystemError>> InterruptLiveUpdates()
    {
        if (_page is null)
            return Result.Failure<SystemError>(SystemError.Of("No browser page is open."));
        var interrupted = await _page.EvaluateAsync<bool>("window.interruptTestNotificationConnection()");
        return interrupted
            ? Result.Success<SystemError>()
            : Result.Failure<SystemError>(SystemError.Of("No active notification connection could be interrupted."));
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
            _page = null;
        }
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
