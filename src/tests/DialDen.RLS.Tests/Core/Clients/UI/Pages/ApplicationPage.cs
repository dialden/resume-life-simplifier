using Microsoft.Playwright;

namespace DialDen.RLS.Tests.Core.Clients.UI.Pages;

public sealed class ApplicationPage(IPage page)
{
    public async Task<Result<VoidValue, SystemError>> Submit(string name, string email, byte[] resume)
    {
        var form = page.GetByTestId("application-form");
        var error = page.GetByRole(AriaRole.Alert);
        var notFound = page.GetByTestId("public-listing-not-found");
        await form.Or(error).Or(notFound).First.WaitForAsync();
        if (await notFound.IsVisibleAsync())
            return Result.Failure<SystemError>(SystemError.Of("Public listing not found."));
        if (await error.IsVisibleAsync())
            return Result.Failure<SystemError>(SystemError.Of(await error.InnerTextAsync()));

        await form.Locator("#application-name").FillAsync(name);
        await form.Locator("#application-email").FillAsync(email);
        var isWord = resume.AsSpan().StartsWith("PK\x03\x04"u8);
        await form.GetByTestId("application-resume").SetInputFilesAsync(new FilePayload
        {
            Name = isWord ? "resume.docx" : "resume.pdf",
            MimeType = isWord ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/pdf",
            Buffer = resume
        });
        await form.GetByTestId("submit-application").ClickAsync();
        var confirmation = page.GetByTestId("application-submitted");
        await confirmation.Or(error).First.WaitForAsync();
        return await confirmation.IsVisibleAsync()
            ? Result.Success<SystemError>()
            : Result.Failure<SystemError>(SystemError.Of(await error.InnerTextAsync()));
    }
}
