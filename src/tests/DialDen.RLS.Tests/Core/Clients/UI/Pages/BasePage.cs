using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using DialDen.RLS.Tests.Core.Clients.UI;
using Microsoft.Playwright;
using PostingStatus = DialDen.RLS.Abstractions.Postings.PostingStatus;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public abstract class BasePage
{
    protected readonly IPage _page;
    protected readonly UIClient? _client;
    internal IPage Page => _page;

    protected BasePage(IPage page, UIClient? client = null)
    {
        _page = page;
        _client = client;
    }

    protected async Task WaitForAsync(ILocator combined, string name, params (string Name, ILocator Locator)[] alternatives)
    {
        try
        {
            await combined.First.WaitForAsync();
        }
        catch (TimeoutException)
        {
            if (_client is not null)
                await _client.RecordWaitTimeoutAsync(name, [("combined", combined), .. alternatives]);
            throw;
        }
    }

    public async Task<HomePage> GoToHomePage()
    {
        if (new Uri(_page.Url).AbsolutePath.StartsWith("/jobs/", StringComparison.Ordinal))
        {
            await _page.GotoAsync("/");
        }
        else
        {
            await _page.GetByRole(AriaRole.Link, new() { Name = "Vue d'ensemble", Exact = true }).ClickAsync();
        }
        await _page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/");
        return new HomePage(_page, _client);
    }

    protected static async Task<IReadOnlyList<JobSitePublicationResponse>> ReadJobSitePublications(ILocator container)
    {
        var publications = new List<JobSitePublicationResponse>();
        foreach (var publication in await container.GetByRole(AriaRole.List, new() { Name = "Publications sur les sites d'emploi", Exact = true })
                     .GetByRole(AriaRole.Listitem).AllAsync())
        {
            var jobSiteId = await publication.GetAttributeAsync("data-job-site-id")
                ?? throw new InvalidOperationException("The job-site publication has no job-site identifier.");
            var displayedStatus = await publication.GetByTestId("job-site-publication-status").InnerTextAsync();
            var status = displayedStatus switch
            {
                "En attente" => "Pending",
                "Réussie" => "Succeeded",
                "Échouée" => "Failed",
                _ => throw new InvalidOperationException($"Unexpected job-site publication status: {displayedStatus}")
            };
            publications.Add(new JobSitePublicationResponse(jobSiteId, status));
        }
        return publications;
    }

    protected static PostingStatus ReadPostingStatus(string displayedStatus) => displayedStatus switch
    {
        "Brouillon" => PostingStatus.Draft,
        "Publiée" => PostingStatus.Published,
        _ => throw new InvalidOperationException($"Unexpected posting status: {displayedStatus}")
    };
}
