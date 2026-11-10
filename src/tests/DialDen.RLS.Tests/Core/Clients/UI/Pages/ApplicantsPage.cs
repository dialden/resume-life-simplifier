using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public sealed class ApplicantsPage(IPage page)
{
    public async Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ObserveApplications(
        Func<IReadOnlyList<ApplicationResponse>, bool> isExpected, TimeSpan timeout)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            var result = await ReadApplications();
            if (result.IsFailure || isExpected(result.Value) || elapsed.Elapsed >= timeout)
                return result;
            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
    }

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
            var extraction = item.GetByTestId("application-extraction-status");
            var extractionStatus = await extraction.CountAsync() == 0
                ? null
                : (await extraction.InnerTextAsync()).Trim() switch
                {
                    "Extracting information\u2026" or "Extraction des informations\u2026" => "Pending",
                    "Information extracted" or "Informations extraites" => "Completed",
                    "Information extraction failed" or "Échec de l'extraction des informations" => "Failed",
                    var displayedStatus => displayedStatus
                };
            var contacts = item.GetByTestId("application-extracted-contacts");
            var extractedContacts = await contacts.CountAsync() == 0
                ? null
                : new ExtractedContactInformation(
                    await ReadContact(contacts, "extracted-name"),
                    await ReadContact(contacts, "extracted-email"),
                    await ReadContact(contacts, "extracted-phone-number"),
                    await ReadContact(contacts, "extracted-postal-address"),
                    await ReadContact(contacts, "extracted-linkedin-url"));
            applications.Add(new ApplicationResponse(
                id,
                await item.GetByTestId("application-name").InnerTextAsync(),
                await item.GetByTestId("application-email").InnerTextAsync(),
                source,
                await item.GetByTestId("application-referral").CountAsync() == 0
                    ? null : await item.GetByTestId("application-referral").InnerTextAsync(),
                extractionStatus, extractedContacts));
        }
        return Result<IReadOnlyList<ApplicationResponse>, SystemError>.Success(applications);
    }

    private static async Task<string?> ReadContact(ILocator contacts, string testId)
    {
        var field = contacts.GetByTestId(testId);
        return await field.CountAsync() == 0 ? null : (await field.InnerTextAsync()).Trim();
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
