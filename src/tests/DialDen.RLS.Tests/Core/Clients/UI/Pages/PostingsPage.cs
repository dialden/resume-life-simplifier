using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class PostingsPage
{
    private readonly IPage _page;

    public PostingsPage(IPage page)
    {
        _page = page;
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        var postings = _page.GetByRole(AriaRole.List, new() { Name = "Postings", Exact = true });
        var empty = _page.GetByRole(AriaRole.Status);
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.Locator("#blazor-error-ui").Filter(new() { Visible = true });
        await postings.Or(empty).Or(alert).Or(errorBanner).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await alert.InnerTextAsync()));
        }
        if (await errorBanner.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await errorBanner.InnerTextAsync()));
        }

        var postingLink = postings.Locator($"a[href='/postings/{id}']");
        if (!await postingLink.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(
                SystemError.Of($"Posting {id} is not in the postings list."));
        }

        await postingLink.ClickAsync();
        await _page.WaitForURLAsync($"**/postings/{id}");
        return await new PostingPage(_page).ReadPosting();
    }
}
