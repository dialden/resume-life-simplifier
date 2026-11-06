using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class PostingPage : BasePage
{
    public PostingPage(IPage page) : base(page)
    {
    }

    /// <summary>Reads the displayed posting repeatedly, without reloading or navigating.</summary>
    public async Task<Result<ViewPostingResponse, SystemError>> ObservePosting(
        Func<ViewPostingResponse, bool> isExpected, TimeSpan timeout)
    {
        var url = _page.Url;
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            var posting = await ReadPosting();
            if (_page.Url != url)
            {
                return Result<ViewPostingResponse, SystemError>.Failure(
                    SystemError.Of($"The page navigated from {url} to {_page.Url} while being observed."));
            }
            if (posting.IsFailure || isExpected(posting.Value) || DateTimeOffset.UtcNow >= deadline)
            {
                return posting;
            }
            await _page.WaitForTimeoutAsync(250);
        }
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ReadPosting()
    {
        var details = _page.GetByRole(AriaRole.Region, new() { Name = "Détails de l'offre | Resume Life Simplifier", Exact = true });
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
        var status = ReadPostingStatus(
            await details.GetByRole(AriaRole.Definition).Nth(2).InnerTextAsync());
        return Result<ViewPostingResponse, SystemError>.Success(
            new ViewPostingResponse(id, title, status)
            {
                Description = await details.GetByTestId("posting-description").InnerTextAsync(),
                JobSitePublications = await ReadJobSitePublications(details)
            });
    }
}
