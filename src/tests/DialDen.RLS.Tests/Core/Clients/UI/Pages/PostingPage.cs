using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;
using PostingStatus = DialDen.RLS.Abstractions.Postings.PostingStatus;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class PostingPage : BasePage
{
    public PostingPage(IPage page) : base(page)
    {
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ReadPosting()
    {
        var details = _page.GetByRole(AriaRole.Region, new() { Name = "Posting details", Exact = true });
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "An unhandled error has occurred." });
        await details.Or(alert).Or(errorBanner).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await alert.InnerTextAsync()));
        }
        if (await errorBanner.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await errorBanner.InnerTextAsync()));
        }

        var definitions = details.GetByRole(AriaRole.Definition);
        var id = int.Parse(await definitions.Nth(0).InnerTextAsync(), CultureInfo.InvariantCulture);
        var title = await details.GetByRole(AriaRole.Heading, new() { Level = 2 }).InnerTextAsync();
        var status = Enum.Parse<PostingStatus>(
            await details.GetByRole(AriaRole.Definition).Nth(2).InnerTextAsync(),
            ignoreCase: false);
        return Result<ViewPostingResponse, SystemError>.Success(
            new ViewPostingResponse(id, title, status) { JobSitePublications = await ReadJobSitePublications(details) });
    }
}
