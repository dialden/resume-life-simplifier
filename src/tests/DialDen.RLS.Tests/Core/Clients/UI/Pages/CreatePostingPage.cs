using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class CreatePostingPage
{
    private readonly IPage _page;

    public CreatePostingPage(IPage page)
    {
        _page = page;
    }

    public async Task<Result<VoidValue, SystemError>> FillTitle(string title)
    {
        var titleField = _page.GetByLabel("Title", new() { Exact = true });
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.Locator("#blazor-error-ui").Filter(new() { Visible = true });
        await titleField.Or(alert).Or(errorBanner).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return Result.Failure<SystemError>(
                SystemError.Of(await alert.InnerTextAsync()));
        }
        if (await errorBanner.IsVisibleAsync())
        {
            return Result.Failure<SystemError>(
                SystemError.Of(await errorBanner.InnerTextAsync()));
        }

        await titleField.FillAsync(title);
        return Result.Success<SystemError>();
    }

    public async Task<Result<CreatePostingResponse, SystemError>> ClickCreatePosting()
    {
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create posting", Exact = true }).ClickAsync();

        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.Locator("#blazor-error-ui").Filter(new() { Visible = true });
        var confirmation = _page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Posting created" });
        await confirmation.Or(alert).Or(errorBanner).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return Result<CreatePostingResponse, SystemError>.Failure(
                SystemError.Of(await alert.InnerTextAsync()));
        }
        if (await errorBanner.IsVisibleAsync())
        {
            return Result<CreatePostingResponse, SystemError>.Failure(
                SystemError.Of(await errorBanner.InnerTextAsync()));
        }

        var createdTitle = await _page.GetByTestId("posting-title").InnerTextAsync();
        var createdId = int.Parse(await _page.GetByTestId("posting-id").InnerTextAsync(), CultureInfo.InvariantCulture);
        return Result<CreatePostingResponse, SystemError>.Success(new CreatePostingResponse(createdId, createdTitle));
    }
}
