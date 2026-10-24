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

        var titleField = _page.GetByLabel("Title", new() { Exact = true });
        if (!await titleField.IsVisibleAsync())
        {
            return Result<CreatePostingResponse, SystemError>.Failure(
                SystemError.Of("The posting creation form is not available."));
        }

        await titleField.FillAsync(title);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create posting", Exact = true }).ClickAsync();

        var confirmation = _page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Posting created" });
        var alert = _page.GetByRole(AriaRole.Alert);
        await confirmation.Or(alert).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return Result<CreatePostingResponse, SystemError>.Failure(SystemError.Of(
                await alert.InnerTextAsync()));
        }

        var createdTitle = await _page.GetByTestId("posting-title").InnerTextAsync();
        return Result<CreatePostingResponse, SystemError>.Success(new CreatePostingResponse(null, createdTitle));
    }
}
