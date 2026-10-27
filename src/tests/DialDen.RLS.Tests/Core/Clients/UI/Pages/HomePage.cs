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
        var postingsPage = await OpenPostings();
        if (postingsPage.IsFailure)
        {
            return Result<CreatePostingResponse, SystemError>.Failure(postingsPage.Error);
        }

        return await postingsPage.Value.CreatePosting(title);
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

        var postingsLink = _page.GetByRole(AriaRole.Link, new() { Name = "View postings", Exact = true });
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
