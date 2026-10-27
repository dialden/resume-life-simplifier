using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using DialDen.RLS.Tests.Fixtures;
using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public class UIClient : IDisposable
{
    private readonly IPlaywright _playwright;
    private readonly RLSConfiguration _configuration;
    private IBrowser? _browser;
    private IPage? _page;

    public UIClient(RLSConfiguration configuration, IPlaywright playwright)
    {
        _configuration = configuration;
        _playwright = playwright;
    }

    public async Task<HomePage> OpenHomePage()
    {
        if (_page == null)
        {
            var browserType = _playwright[PlaywrightSettingsProvider.BrowserName];
            _browser = await browserType.LaunchAsync();
            _page = await _browser.NewPageAsync(new BrowserNewPageOptions { BaseURL = _configuration.UIUrl });
            await _page.GotoAsync(string.Empty);
            return new HomePage(_page);
        }

        return await new HomePage(_page).GoToHomePage();
    }

    public void Dispose()
    {
        _browser?.CloseAsync().GetAwaiter().GetResult();
    }
}
