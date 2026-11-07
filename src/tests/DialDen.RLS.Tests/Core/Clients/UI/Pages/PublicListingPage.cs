using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public sealed class PublicListingPage(IPage page)
{
    public async Task<Result<PublicListingResponse, SystemError>> ReadListing()
    {
        var listing = page.Locator("section[data-posting-id]");
        var notFound = page.GetByTestId("public-listing-not-found");
        var alert = page.GetByRole(AriaRole.Alert).Filter(new() { Visible = true });
        await listing.Or(notFound).Or(alert).First.WaitForAsync();
        if (await notFound.IsVisibleAsync())
        {
            return Result<PublicListingResponse, SystemError>.Failure(SystemError.Of("Public listing not found."));
        }
        if (await alert.IsVisibleAsync())
        {
            return Result<PublicListingResponse, SystemError>.Failure(SystemError.Of(await alert.InnerTextAsync()));
        }

        var id = int.Parse(await listing.GetAttributeAsync("data-posting-id")
            ?? throw new InvalidOperationException("The public listing has no posting identifier."),
            CultureInfo.InvariantCulture);
        var title = await listing.GetByRole(AriaRole.Heading, new() { Level = 1 }).InnerTextAsync();
        var description = await listing.GetByTestId("public-listing-description").InnerTextAsync();
        var canonicalLink = page.Locator("head link[rel='canonical']");
        var address = await canonicalLink.GetAttributeAsync("href")
            ?? throw new InvalidOperationException("The public listing has no canonical address.");
        return Result<PublicListingResponse, SystemError>.Success(
            new PublicListingResponse(id, title, description) { PublicListingUrl = new Uri(address, UriKind.Absolute) });
    }
}
