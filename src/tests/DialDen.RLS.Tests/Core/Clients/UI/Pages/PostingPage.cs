using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class PostingPage
{
    private readonly IPage _page;

    public PostingPage(IPage page)
    {
        _page = page;
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ReadPosting()
    {
        var details = _page.GetByRole(AriaRole.Region, new() { Name = "Posting details", Exact = true });
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.Locator("#blazor-error-ui").Filter(new() { Visible = true });
        await details.Or(alert).Or(errorBanner).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await alert.InnerTextAsync()));
        }
        if (await errorBanner.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await errorBanner.InnerTextAsync()));
        }

        var id = int.Parse(await details.GetByTestId("posting-id").InnerTextAsync(), CultureInfo.InvariantCulture);
        var title = await details.GetByTestId("posting-title").InnerTextAsync();
        return Result<ViewPostingResponse, SystemError>.Success(new ViewPostingResponse(id, title));
    }
}
