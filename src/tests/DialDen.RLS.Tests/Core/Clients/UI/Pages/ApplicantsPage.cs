using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public sealed class ApplicantsPage(IPage page)
{
    public async Task<Result<ComparisonResponse, SystemError>> CompareCandidates(IReadOnlyList<int> applicationIds)
    {
        foreach (var id in applicationIds)
            await page.Locator($"[data-application-id='{id}']").GetByRole(AriaRole.Checkbox).CheckAsync();
        await page.GetByTestId("compare-candidates").ClickAsync();
        var requirement = await page.GetByTestId("comparison-requirement").InnerTextAsync();
        var candidates = new List<CandidateComparison>();
        foreach (var id in applicationIds)
        {
            var column = page.Locator($"[data-comparison-id='{id}']");
            var name = await column.GetByTestId("comparison-name").InnerTextAsync();
            var cell = page.Locator($"[data-comparison-evidence-id='{id}']");
            await cell.Locator("summary").ClickAsync();
            var evidence = await cell.Locator("blockquote").CountAsync() == 0
                ? null : await cell.Locator("blockquote").InnerTextAsync();
            candidates.Add(new(name, evidence));
        }
        return Result<ComparisonResponse, SystemError>.Success(new(requirement, candidates));
    }

    public async Task<Result<VoidValue, SystemError>> ReviewEducation(int applicationId, bool? overrideMet, string note)
    {
        var item = page.Locator($"[data-application-id='{applicationId}']");
        await item.GetByTestId("education-review-details").Locator("summary").ClickAsync();
        await item.GetByTestId("education-review-note").FillAsync(note);
        await item.GetByTestId(overrideMet switch
        {
            true => "education-review-met",
            false => "education-review-not-met",
            null => "education-review-reset"
        }).ClickAsync();
        var saved = item.GetByTestId("education-review-saved");
        var error = item.GetByTestId("education-review-error");
        await saved.Or(error).First.WaitForAsync();
        return await saved.IsVisibleAsync()
            ? Result.Success<SystemError>()
            : Result.Failure<SystemError>(SystemError.Of(await error.InnerTextAsync()));
    }

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
            var details = item.GetByTestId("applicant-details");
            if (await details.GetAttributeAsync("open") is null)
                await details.Locator("summary").First.ClickAsync();
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
                    "Extracting information\u2026" or "Extraction des informations\u2026" => "AwaitingExtraction",
                    "Information extracted" or "Informations extraites" => "Ready",
                    "Information extraction failed" or "Échec de l'extraction des informations" => "ExtractionFailed",
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
            var education = item.GetByTestId("application-extracted-education");
            List<ExtractedEducation>? extractedEducation = null;
            if (await education.CountAsync() != 0)
            {
                extractedEducation = [];
                foreach (var entry in await education.GetByTestId("extracted-education-entry").AllAsync())
                {
                    extractedEducation.Add(new ExtractedEducation(
                        await ReadContact(entry, "education-institution"),
                        await ReadContact(entry, "education-qualification"),
                        await ReadContact(entry, "education-start-date"),
                        await ReadContact(entry, "education-end-date")));
                }
            }
            var confidenceText = await item.GetAttributeAsync("data-education-confidence");
            var assessment = confidenceText is null ? null : new EducationAssessment(
                bool.TryParse(await item.GetAttributeAsync("data-education-original"), out var original) ? original : null,
                double.Parse(confidenceText, CultureInfo.InvariantCulture),
                await item.GetAttributeAsync("data-education-evidence"),
                bool.TryParse(await item.GetAttributeAsync("data-education-override"), out var verdict) ? verdict : null,
                await item.GetAttributeAsync("data-education-note") ?? "");
            applications.Add(new ApplicationResponse(
                id,
                await item.GetByTestId("application-name").InnerTextAsync(),
                await item.GetByTestId("application-email").InnerTextAsync(),
                source,
                await item.GetByTestId("application-referral").CountAsync() == 0
                    ? null : await item.GetByTestId("application-referral").InnerTextAsync(),
                extractionStatus, extractedContacts, extractedEducation,
                await item.GetAttributeAsync("data-review-group"), assessment,
                int.Parse(await item.GetAttributeAsync("data-review-version")
                    ?? throw new InvalidOperationException("The application has no review version."),
                    CultureInfo.InvariantCulture)));
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
