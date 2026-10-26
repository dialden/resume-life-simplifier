using Microsoft.Playwright;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class HomePage
{
    private readonly IPage _page;

    public HomePage(IPage page)
    {
        _page = page;
    }

    /// <summary>
    /// Waits for the home page to finish loading the API entry point, then returns the error shown
    /// (the page's alert or Blazor's unhandled error banner), or null if the page loaded fine.
    /// </summary>
    public async Task<string?> GetError()
    {
        var heading = _page.GetByRole(AriaRole.Heading);
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.Locator("#blazor-error-ui").Filter(new() { Visible = true });
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

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title)
    {
        var error = await GetError();
        if (error is not null)
        {
            return Result<CreatePostingResponse, SystemError>.Failure(SystemError.Of(error));
        }

        var createLink = _page.GetByRole(AriaRole.Link, new() { Name = "Create posting", Exact = true });
        if (!await createLink.IsVisibleAsync())
        {
            return Result<CreatePostingResponse, SystemError>.Failure(
                SystemError.Of("The link to the posting creation page is not available."));
        }

        if (await _page.GetByLabel("Title", new() { Exact = true }).IsVisibleAsync())
        {
            return Result<CreatePostingResponse, SystemError>.Failure(
                SystemError.Of("The posting creation form should be on its own page, not the home page."));
        }

        await createLink.ClickAsync();
        await _page.WaitForURLAsync("**/postings/create");
        var createPostingPage = new CreatePostingPage(_page);
        var fillTitleResult = await createPostingPage.FillTitle(title);
        if (fillTitleResult.IsFailure)
        {
            return Result<CreatePostingResponse, SystemError>.Failure(fillTitleResult.Error);
        }

        return await createPostingPage.ClickCreatePosting();
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        var error = await GetError();
        if (error is not null)
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(error));
        }

        var postingsLink = _page.GetByRole(AriaRole.Link, new() { Name = "View postings", Exact = true });
        if (!await postingsLink.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(
                SystemError.Of("The link to the postings list is not available."));
        }

        await postingsLink.ClickAsync();
        await _page.WaitForURLAsync("**/postings");
        return await new PostingsPage(_page).ViewPosting(id);
    }
}
