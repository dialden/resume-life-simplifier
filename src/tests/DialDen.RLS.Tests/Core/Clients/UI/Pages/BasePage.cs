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
