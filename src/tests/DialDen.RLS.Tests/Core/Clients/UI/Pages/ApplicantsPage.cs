using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public sealed class ApplicantsPage(IPage page)
{
    public async Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ReadApplications()
    {
        var list = page.GetByTestId("applications");
        var error = page.GetByRole(AriaRole.Alert);
        await list.Or(error).First.WaitForAsync();
        if (!await list.IsVisibleAsync())
        {
            return Result<IReadOnlyList<ApplicationResponse>, SystemError>.Failure(
                SystemError.Of(await error.InnerTextAsync()));
        }

        var applications = new List<ApplicationResponse>();
        foreach (var item in await list.Locator("[data-application-id]").AllAsync())
        {
            var id = int.Parse(await item.GetAttributeAsync("data-application-id")
                ?? throw new InvalidOperationException("The application has no identifier."),
                CultureInfo.InvariantCulture);
            var displayedSource = await item.GetByTestId("application-source").InnerTextAsync();
            var source = displayedSource switch
            {
                "Site Web du client" => "Client website",
                "Client website" => "Client website",
                _ => displayedSource
            };
            applications.Add(new ApplicationResponse(
                id,
                await item.GetByTestId("application-name").InnerTextAsync(),
                await item.GetByTestId("application-email").InnerTextAsync(),
                source,
                await item.GetByTestId("application-referral").CountAsync() == 0
                    ? null : await item.GetByTestId("application-referral").InnerTextAsync()));
        }
        return Result<IReadOnlyList<ApplicationResponse>, SystemError>.Success(applications);
    }

    public async Task<Result<byte[], SystemError>> DownloadResume(int applicationId)
    {
        var item = page.GetByTestId("applications").Locator($"[data-application-id='{applicationId}']");
        var download = await page.RunAndWaitForDownloadAsync(
            () => item.GetByTestId("download-resume").ClickAsync());
        var failure = await download.FailureAsync();
        if (failure is not null)
        {
            return Result<byte[], SystemError>.Failure(SystemError.Of($"Downloading a resume failed: {failure}"));
        }
        await using var stream = await download.CreateReadStreamAsync();
        using var output = new MemoryStream();
        await stream.CopyToAsync(output);
        return Result<byte[], SystemError>.Success(output.ToArray());
    }
}
