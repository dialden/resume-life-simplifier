using System.Globalization;
using System.Text.RegularExpressions;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;
using PostingStatus = DialDen.RLS.Abstractions.Postings.PostingStatus;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public class PostingsPage : BasePage
{
    public PostingsPage(IPage page) : base(page)
    {
    }

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title)
    {
        await _page.GetByRole(AriaRole.Link, new() { Name = "Create posting", Exact = true }).ClickAsync();
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
        var postings = _page.GetByRole(AriaRole.List, new() { Name = "Postings", Exact = true });
        var empty = _page.GetByRole(AriaRole.Status).Filter(new() { HasText = "No postings yet." });
        var alert = _page.GetByRole(AriaRole.Alert);
        var errorBanner = _page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "An unhandled error has occurred." });
        await postings.Or(empty).Or(alert).Or(errorBanner).First.WaitForAsync();

        if (await alert.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await alert.InnerTextAsync()));
        }
        if (await errorBanner.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(await errorBanner.InnerTextAsync()));
        }

        var postingLink = await FindPostingLink(id);
        if (!await postingLink.IsVisibleAsync())
        {
            return Result<ViewPostingResponse, SystemError>.Failure(
                SystemError.Of($"Posting {id} is not in the postings list."));
        }

        await postingLink.ClickAsync();
        await _page.WaitForURLAsync($"**/postings/{id}");
        return await new PostingPage(_page).ReadPosting();
    }

    public async Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id)
    {
        var postingLink = await FindPostingLink(id);
        if (!await postingLink.IsVisibleAsync())
        {
            return Result<PublishPostingResponse, SystemError>.Failure(
                SystemError.Of($"Posting {id} is not in the postings list."));
        }

        var title = await postingLink.GetByRole(AriaRole.Heading, new() { Level = 3 }).InnerTextAsync();
        await _page.GetByRole(AriaRole.Radio, new() { Name = $"Select posting {title}, ID {id}" }).ClickAsync();

        var status = _page.GetByTestId($"posting-status-{id}");
        var alert = _page.GetByRole(AriaRole.Alert);
        var response = await _page.RunAndWaitForResponseAsync(
            () => _page.GetByRole(AriaRole.Button, new() { Name = "Publish selected", Exact = true }).ClickAsync(),
            response => response.Request.Method == "POST" &&
                        new Uri(response.Url).AbsolutePath == $"/postings/{id}/publish");
        if (!response.Ok)
        {
            await alert.WaitForAsync();
            return Result<PublishPostingResponse, SystemError>.Failure(
                SystemError.Of(await alert.InnerTextAsync()));
        }

        await status.Filter(new() { HasText = "Published" }).WaitForAsync();
        if (await alert.IsVisibleAsync())
        {
            return Result<PublishPostingResponse, SystemError>.Failure(
                SystemError.Of(await alert.InnerTextAsync()));
        }

        var postingStatus = Enum.Parse<PostingStatus>(await status.InnerTextAsync(), ignoreCase: false);
        return Result<PublishPostingResponse, SystemError>.Success(new PublishPostingResponse(id, postingStatus));
    }

    public async Task<Result<PostingPageResponse, SystemError>> ReadPage(int pageNumber)
    {
        var pagination = _page.GetByTestId("posting-pagination");
        var alert = _page.GetByRole(AriaRole.Alert);
        await pagination.Or(alert).First.WaitForAsync();
        if (!await alert.IsVisibleAsync())
        {
            var totalCount = ReadTotalCount(await _page.GetByRole(AriaRole.Status)
                .Filter(new() { HasText = "Showing" }).InnerTextAsync());
            if (pageNumber < 1 || (long)(pageNumber - 1) * 10 >= totalCount && pageNumber > 1)
            {
                await _page.GotoAsync($"/postings?page={pageNumber.ToString(CultureInfo.InvariantCulture)}");
                await pagination.Or(alert).First.WaitForAsync();
            }
        }
        while (!await alert.IsVisibleAsync())
        {
            var current = int.Parse(await pagination.GetAttributeAsync("data-page-number")
                ?? throw new InvalidOperationException("The page number was not displayed."), CultureInfo.InvariantCulture);
            if (current == pageNumber)
            {
                break;
            }
            await MoveToAdjacentPage(current, pageNumber > current);
        }
        if (await alert.IsVisibleAsync())
        {
            return Result<PostingPageResponse, SystemError>.Failure(SystemError.Of(await alert.InnerTextAsync()));
        }

        var items = new List<ViewPostingResponse>();
        foreach (var row in await _page.GetByRole(AriaRole.List, new() { Name = "Postings", Exact = true })
                     .GetByRole(AriaRole.Link).AllAsync())
        {
            var href = await row.GetAttributeAsync("href")
                ?? throw new InvalidOperationException("A posting row has no destination.");
            var id = int.Parse(href.Split('/').Last(), CultureInfo.InvariantCulture);
            var title = await row.GetByRole(AriaRole.Heading, new() { Level = 3 }).InnerTextAsync();
            var status = Enum.Parse<PostingStatus>(
                await row.GetByTestId($"posting-status-{id}").InnerTextAsync(),
                ignoreCase: false);
            items.Add(new ViewPostingResponse(id, title, status));
        }

        var total = ReadTotalCount(await _page.GetByRole(AriaRole.Status)
            .Filter(new() { HasText = "Showing" }).InnerTextAsync());
        var currentPage = int.Parse(await pagination.GetAttributeAsync("data-page-number")
            ?? throw new InvalidOperationException("The page number was not displayed."), CultureInfo.InvariantCulture);
        var pageSize = int.Parse(await pagination.GetAttributeAsync("data-page-size")
            ?? throw new InvalidOperationException("The page size was not displayed."), CultureInfo.InvariantCulture);
        var previous = _page.GetByRole(AriaRole.Link, new() { Name = "Previous page", Exact = true });
        var next = _page.GetByRole(AriaRole.Link, new() { Name = "Next page", Exact = true });
        var disabledPrevious = _page.GetByRole(AriaRole.Button, new() { Name = "Previous page", Exact = true });
        var disabledNext = _page.GetByRole(AriaRole.Button, new() { Name = "Next page", Exact = true });
        var previousIsDisabled = await disabledPrevious.IsVisibleAsync() && await disabledPrevious.IsDisabledAsync();
        var nextIsDisabled = await disabledNext.IsVisibleAsync() && await disabledNext.IsDisabledAsync();
        if (await previous.IsVisibleAsync() != (currentPage > 1) ||
            await next.IsVisibleAsync() != ((long)currentPage * pageSize < total) ||
            previousIsDisabled != (currentPage == 1) ||
            nextIsDisabled != ((long)currentPage * pageSize >= total))
        {
            throw new InvalidOperationException("The paging controls do not match the available pages.");
        }

        return Result<PostingPageResponse, SystemError>.Success(new(items, total, currentPage, pageSize));
    }

    public async Task<ILocator> FindPostingLink(int id)
    {
        var postings = _page.GetByRole(AriaRole.List, new() { Name = "Postings", Exact = true });
        var link = postings.GetByRole(AriaRole.Link, new()
        {
            NameRegex = new Regex($@", ID {id}$")
        });
        var pagination = _page.GetByTestId("posting-pagination");
        await pagination.WaitForAsync();
        while (!await link.IsVisibleAsync())
        {
            var next = _page.GetByRole(AriaRole.Link, new() { Name = "Next page", Exact = true });
            if (!await next.IsVisibleAsync())
            {
                break;
            }
            var current = int.Parse(await pagination.GetAttributeAsync("data-page-number")
                ?? throw new InvalidOperationException("The page number was not displayed."), CultureInfo.InvariantCulture);
            await MoveToAdjacentPage(current, true);
            var alert = _page.GetByRole(AriaRole.Alert);
            if (await alert.IsVisibleAsync())
            {
                throw new InvalidOperationException(await alert.InnerTextAsync());
            }
        }
        return link;
    }

    private async Task MoveToAdjacentPage(int current, bool forward)
    {
        var target = forward ? current + 1 : current - 1;
        await _page.GetByRole(AriaRole.Link, new()
        {
            Name = forward ? "Next page" : "Previous page", Exact = true
        }).ClickAsync();
        await _page.Locator($"[data-testid='posting-pagination'][data-page-number='{target}']")
            .Or(_page.GetByRole(AriaRole.Alert)).First.WaitForAsync();
    }

    private static long ReadTotalCount(string summary)
    {
        var match = Regex.Match(summary, @"\bof (?<total>\d+) postings?\b", RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            throw new InvalidOperationException($"The postings summary had an unexpected format: {summary}");
        }

        return long.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture);
    }
}
