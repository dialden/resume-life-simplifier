using Microsoft.Playwright;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class HomePage : BasePage
{
    public HomePage(IPage page) : base(page)
    {
    }

    public async Task SelectLanguage(string language)
    {
        var button = language switch
        {
            "fr" => _page.GetByTestId("language-fr"),
            "en" => _page.GetByTestId("language-en"),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Supported languages are 'en' and 'fr'.")
        };
        await button.ClickAsync();
        await _page.WaitForFunctionAsync(
            "(expectedLanguage) => document.documentElement.lang === expectedLanguage", language);
    }

    public Task<string> ReadWorkspaceTitle() => _page.Locator(".topbar-title").InnerTextAsync();

    public Task WaitForBrandLogo() => _page.Locator(".brand-logo-full").WaitForAsync();

    public Task<string?> ReadBrandLogoPath() => _page.Locator(".brand-logo-full").GetAttributeAsync("src");

    public Task<string?> ReadBrandLogoAltText() => _page.Locator(".brand-logo-full").GetAttributeAsync("alt");

    public Task<string?> ReadFaviconPath() => _page.Locator("link[rel='icon']").GetAttributeAsync("href");

    public Task<string> ReadSidebarBackgroundColor() =>
        _page.Locator(".workspace-sidebar").EvaluateAsync<string>(
            "element => getComputedStyle(element).backgroundColor");

    public Task<string> ReadPrimaryButtonBackgroundColor() =>
        _page.Locator(".language-switcher .mud-button-filled-primary").EvaluateAsync<string>(
            "element => getComputedStyle(element).backgroundColor");

    public Task<string> ReadPrimaryButtonFontFamily() =>
        _page.Locator(".language-switcher .mud-button-filled-primary").EvaluateAsync<string>(
            "element => getComputedStyle(element).fontFamily");

    public Task<int> LoadInterFontsFor(string text) =>
        _page.EvaluateAsync<int>(
            "async text => (await document.fonts.load('16px Inter', text)).length",
            text);

    public async Task<string> ReadDocumentLanguage() =>
        await _page.Locator("html").GetAttributeAsync("lang")
        ?? throw new InvalidOperationException("The document language was not set.");

    /// <summary>
    /// Waits for the home page to finish loading the API entry point, then returns the error shown
    /// (the page's alert or Blazor's unhandled error banner), or null if the page loaded fine.
    /// </summary>
    public async Task<string?> GetError()
    {
        var heading = _page.GetByRole(AriaRole.Heading);
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "An unhandled error has occurred." });
        // The page ends up in exactly one of three states; wait for whichever comes first
        await heading.Or(alert).Or(errorBanner).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return (await alert.TextContentAsync())?.Trim();
        }
        if (await errorBanner.IsVisibleAsync())
        {
            return (await errorBanner.TextContentAsync())?.Trim();
        }
        return null;
    }

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title, string description, string? requiredEducation)
    {
        var postingsPage = await OpenPostings();
        if (postingsPage.IsFailure)
        {
            return Result<CreatePostingResponse, SystemError>.Failure(postingsPage.Error);
        }

        return await postingsPage.Value.CreatePosting(title, description, requiredEducation);
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        var postingsPage = await OpenPostings();
        if (postingsPage.IsFailure)
        {
            return Result<ViewPostingResponse, SystemError>.Failure(postingsPage.Error);
        }

        return await postingsPage.Value.ViewPosting(id);
    }

    public async Task<Result<PostingsPage, SystemError>> OpenPostings()
    {
        var error = await GetError();
        if (error is not null)
        {
            return Result<PostingsPage, SystemError>.Failure(SystemError.Of(error));
        }

        var postingsLink = _page.GetByRole(AriaRole.Link, new() { Name = "Voir les offres", Exact = true });
        if (!await postingsLink.IsVisibleAsync())
        {
            return Result<PostingsPage, SystemError>.Failure(
                SystemError.Of("The link to the postings list is not available."));
        }

        await postingsLink.ClickAsync();
        await _page.WaitForURLAsync("**/postings");
        return Result<PostingsPage, SystemError>.Success(new PostingsPage(_page));
    }

}
