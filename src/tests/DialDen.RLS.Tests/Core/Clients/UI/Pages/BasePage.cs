using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
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

    protected static async Task<IReadOnlyList<PostingDeliveryResponse>> ReadDeliveries(ILocator container)
    {
        var deliveries = new List<PostingDeliveryResponse>();
        foreach (var delivery in await container.GetByRole(AriaRole.List, new() { Name = "External deliveries", Exact = true })
                     .GetByRole(AriaRole.Listitem).AllAsync())
        {
            var jobSiteId = await delivery.GetAttributeAsync("data-job-site-id")
                ?? throw new InvalidOperationException("The delivery has no job-site identifier.");
            deliveries.Add(new PostingDeliveryResponse(
                jobSiteId, await delivery.GetByTestId("delivery-status").InnerTextAsync()));
        }
        return deliveries;
    }
}
