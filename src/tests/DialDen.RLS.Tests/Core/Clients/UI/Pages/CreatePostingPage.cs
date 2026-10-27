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
        var response = await _page.RunAndWaitForResponseAsync(
            () => _page.GetByRole(AriaRole.Button, new() { Name = "Create posting", Exact = true }).ClickAsync(),
            response => response.Request.Method == "POST" && new Uri(response.Url).AbsolutePath == "/postings");

        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.Locator("#blazor-error-ui").Filter(new() { Visible = true });
        var postings = _page.GetByRole(AriaRole.List, new() { Name = "Postings", Exact = true });
        await postings.Or(alert).Or(errorBanner).First.WaitForAsync();

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

        var createdPosting = await response.JsonAsync()
            ?? throw new InvalidOperationException("The API returned no created posting.");
        var createdId = createdPosting.GetProperty("id").GetInt32();
        var postingLink = await new PostingsPage(_page).FindPostingLink(createdId);
        var createdTitle = await postingLink.GetByTestId("posting-title").InnerTextAsync();
        return Result<CreatePostingResponse, SystemError>.Success(new CreatePostingResponse(createdId, createdTitle));
    }
}
