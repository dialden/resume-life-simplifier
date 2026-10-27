using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public abstract class BasePage
{
    protected readonly IPage _page;

    protected BasePage(IPage page)
    {
        _page = page;
    }

    public async Task<HomePage> GoToHomePage()
    {
        await _page.GetByRole(AriaRole.Link, new() { Name = "Overview", Exact = true }).ClickAsync();
        await _page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/");
        return new HomePage(_page);
    }
}
