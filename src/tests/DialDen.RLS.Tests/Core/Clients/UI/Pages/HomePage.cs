using Microsoft.Playwright;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class HomePage : BasePage
{
    public HomePage(IPage page) : base(page)
    {
    }

    /// <summary>
    /// Waits for the home page to finish loading the API entry point, then returns the error shown
    /// (the page's alert or Blazor's unhandled error banner), or null if the page loaded fine.
    /// </summary>
    public async Task<string?> GetError()
    {
        var heading = _page.GetByRole(AriaRole.Heading);
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.Locator("#blazor-error-ui");
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

    public Task<string> ReadTitle() =>
        _page.GetByRole(AriaRole.Heading, new() { Level = 1 }).InnerTextAsync();

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title, string description)
    {
        var postingsPage = await OpenPostings();
        if (postingsPage.IsFailure)
        {
            return Result<CreatePostingResponse, SystemError>.Failure(postingsPage.Error);
        }

        return await postingsPage.Value.CreatePosting(title, description);
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

        var postingsLink = _page.GetByTestId("view-postings-link");
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
