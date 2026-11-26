using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using DialDen.RLS.Tests.Fixtures;
using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public class UIClient : IUIClient, IAsyncDisposable
{
    private readonly Func<Task<IBrowser>> _getBrowser;
    private readonly RLSConfiguration _configuration;
    private readonly ScenarioDiagnostics _diagnostics;
    private readonly ConcurrentDictionary<IRequest, string> _pendingRequests = new();
    private IBrowserContext? _context;
    private IPage? _page;
    private int _notificationStreamRequests;

    internal UIClient(RLSConfiguration configuration, Func<Task<IBrowser>> getBrowser, ScenarioDiagnostics diagnostics)
    {
        _configuration = configuration;
        _getBrowser = getBrowser;
        _diagnostics = diagnostics;
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

    public async Task<string> ReadLoadingPercentageBeforeBlazorStarts(int? percentage = null)
    {
        var page = _page ?? throw new InvalidOperationException("The startup loading screen has not been opened.");
        if (percentage is not null)
        {
            await page.EvaluateAsync(
                """value => document.documentElement.style.setProperty('--blazor-load-percentage-text', `"${value}%"`)""",
                percentage.Value);
        }
        return await page.Locator("#app .loading-percentage").EvaluateAsync<string>(
            """element => getComputedStyle(element, "::after").content.replace(/^"|"$/g, '')""");
    }

    public Uri PublicWebsiteBaseUrl => new(_configuration.UIUrl);

    public async Task<string> ReadPublicPageHeadingInLanguage(string path, string language)
    {
        var page = _page ?? await CreatePage();
        if (new Uri(page.Url).AbsolutePath != path)
        {
            await page.GotoAsync(path);
        }
        await page.GetByRole(AriaRole.Heading, new() { Level = 1 }).WaitForAsync();
        await new HomePage(page).SelectLanguage(language);
        return await page.GetByRole(AriaRole.Heading, new() { Level = 1 }).InnerTextAsync();
    }

    public async Task<bool> ShowsWorkspaceNavigationAt(string path)
    {
        var page = _page ?? await CreatePage();
        await page.GotoAsync(path);
        await page.GetByRole(AriaRole.Heading, new() { Level = 1 }).WaitForAsync();
        return await page.Locator(".workspace-sidebar").IsVisibleAsync();
    }

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
        _context ??= await (await _getBrowser()).NewContextAsync(
            new BrowserNewContextOptions { BaseURL = _configuration.UIUrl });
        var page = await _context.NewPageAsync();
        page.PageError += (_, _) => _diagnostics.Record("Browser", "Unhandled page error (details omitted)");
        page.Console += (_, message) =>
        {
            if (message.Type is "error" or "warning")
                _diagnostics.Record("Browser", $"Console {message.Type} (details omitted)");
        };
        page.RequestFinished += (sender, request) => _pendingRequests.TryRemove(request, out _);
        page.RequestFailed += (sender, request) =>
        {
            _pendingRequests.TryRemove(request, out _);
            _diagnostics.Record("Browser", $"Request failed: resource={request.ResourceType}, " +
                $"route={ClassifyRoute(request.Url)}, error={ClassifyFailure(request.Failure)}");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
                _diagnostics.Record("Browser", $"HTTP {response.Status}: resource={response.Request.ResourceType}, " +
                    $"route={ClassifyRoute(response.Url)}");
        };
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
            _pendingRequests[request] = $"{request.ResourceType}:{ClassifyRoute(request.Url)}";
            if (new Uri(request.Url).AbsolutePath == "/notifications/stream")
            {
                Interlocked.Increment(ref _notificationStreamRequests);
            }
        };
        _page = page;
        return page;
    }

    internal async Task RecordDiagnosticsAsync()
    {
        if (_page is null)
        {
            _diagnostics.Record("Browser", "No page was opened");
            return;
        }
        var page = _page;
        var path = ClassifyRoute(page.Url);
        var pending = _pendingRequests.Values.GroupBy(value => value)
            .Select(group => $"{group.Key}={group.Count()}");
        _diagnostics.Record("Browser", $"Pending requests: {string.Join(", ", pending)}");
        try
        {
            var state = await page.EvaluateAsync<string>("""
                () => JSON.stringify({
                    readyState: document.readyState,
                    appChildren: document.querySelector('#app')?.childElementCount ?? 0,
                    headings: document.querySelectorAll('h1,h2,h3').length,
                    alerts: document.querySelectorAll('[role=alert]').length,
                    statuses: document.querySelectorAll('[role=status]').length
                })
                """).WaitAsync(TimeSpan.FromSeconds(3));
            _diagnostics.Record("Browser", $"path={path}, state={state}");
        }
        catch (Exception error) when (error is PlaywrightException or TimeoutException)
        {
            _diagnostics.Record("Browser", $"path={path}, page state unavailable: {error.GetType().Name}");
        }
    }

    private static string ClassifyRoute(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address))
            return "(unavailable)";
        var segment = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return segment switch
        {
            null => "/",
            "_framework" or "_content" or "postings" or "jobs" or "notifications" => "/" + segment,
            _ => "(other)"
        };
    }

    private static string ClassifyFailure(string? failure) =>
        failure is not null && failure.StartsWith("net::ERR_", StringComparison.Ordinal)
            && failure.AsSpan("net::ERR_".Length).ToString().All(character => char.IsAsciiLetterUpper(character) || character == '_')
            ? failure
            : "(omitted)";

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
        if (_context is not null)
        {
            await _context.CloseAsync();
            _context = null;
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
