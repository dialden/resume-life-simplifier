using DialDen.RLS.Tests.Core.Drivers.RLS;
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
        await _page.GetByTestId("overview-link").ClickAsync();
        await _page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/");
        return new HomePage(_page);
    }

    public async Task ChooseLanguage(Language language)
    {
        var (name, tag) = language == Language.French ? ("Français", "fr") : ("English", "en");
        var button = _page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
        if (await button.GetAttributeAsync("aria-pressed") == "true")
        {
            return;
        }

        await button.ClickAsync();
        // The workspace reloads in the chosen language.
        await _page.Locator($"html[lang='{tag}']").WaitForAsync(new() { State = WaitForSelectorState.Attached });
    }

    protected static async Task<IReadOnlyList<JobSitePublicationResponse>> ReadJobSitePublications(ILocator container)
    {
        var publications = new List<JobSitePublicationResponse>();
        foreach (var publication in await container.GetByRole(AriaRole.List, new() { Name = "Job-site publications", Exact = true })
                     .GetByRole(AriaRole.Listitem).AllAsync())
        {
            var jobSiteId = await publication.GetAttributeAsync("data-job-site-id")
                ?? throw new InvalidOperationException("The job-site publication has no job-site identifier.");
            publications.Add(new JobSitePublicationResponse(
                jobSiteId, await publication.GetByTestId("job-site-publication-status").InnerTextAsync()));
        }
        return publications;
    }
}
