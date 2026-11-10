using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DialDen.RLS.Infrastructure.Applications;

internal sealed class ResumePageImageRenderer(ILogger<ResumePageImageRenderer> logger)
{
    private const int MaximumPages = 20;
    private const int MaximumImageBytes = 5 * 1024 * 1024;
    private static readonly TimeSpan RenderingTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);

    public async Task<IReadOnlyList<byte[]>> Render(
        string fileName, byte[] resume, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".pdf" or ".docx"))
            throw new InvalidDataException("Resume rendering supports only PDF and DOCX documents.");

        using var rendering = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        rendering.CancelAfter(RenderingTimeout);
        var directory = Directory.CreateTempSubdirectory("rls-resume-");
        try
        {
            var inputPath = Path.Combine(directory.FullName, $"resume{extension}");
            await File.WriteAllBytesAsync(inputPath, resume, rendering.Token);
            var pdfPath = Path.Combine(directory.FullName, "resume.pdf");
            if (extension == ".docx")
            {
                var profile = new Uri(Path.Combine(directory.FullName, "office-profile")).AbsoluteUri;
                await Run("libreoffice",
                    [$"-env:UserInstallation={profile}", "--headless", "--norestore",
                        "--convert-to", "pdf:writer_pdf_Export", "--outdir", directory.FullName, inputPath],
                    rendering.Token);
                if (!File.Exists(pdfPath))
                    throw new InvalidDataException("Resume conversion did not produce a PDF document.");
            }

            var information = await Run("pdfinfo", [pdfPath], rendering.Token);
            var pagesLine = information.Split('\n')
                .SingleOrDefault(line => line.StartsWith("Pages:", StringComparison.Ordinal));
            if (pagesLine is null || !int.TryParse(pagesLine.AsSpan("Pages:".Length).Trim(),
                    NumberStyles.None, CultureInfo.InvariantCulture, out var pages) || pages < 1)
                throw new InvalidDataException("The resume page count could not be determined.");
            if (pages > MaximumPages)
                throw new InvalidDataException($"Resume information extraction supports at most {MaximumPages} pages.");

            var images = new List<byte[]>(pages);
            for (var page = 1; page <= pages; page++)
            {
                var number = page.ToString(CultureInfo.InvariantCulture);
                var output = Path.Combine(directory.FullName, $"page-{number}");
                await Run("pdftoppm",
                    ["-f", number, "-l", number, "-singlefile", "-scale-to", "1600",
                        "-jpeg", "-jpegopt", "quality=85", pdfPath, output], rendering.Token);
                var image = new FileInfo($"{output}.jpg");
                if (!image.Exists || image.Length is 0 or > MaximumImageBytes)
                    throw new InvalidDataException("Resume rendering did not produce an image within the supported size.");
                images.Add(await File.ReadAllBytesAsync(image.FullName, rendering.Token));
                image.Delete();
            }
            return images;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Resume rendering exceeded its time limit.", exception);
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(exception, "Could not remove temporary resume rendering directory {Directory}.",
                    directory.FullName);
            }
        }
    }

    private static async Task<string> Run(
        string executable, string[] arguments, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProcessTimeout);
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["LC_ALL"] = "C";
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {executable} for resume rendering.");
        var output = ReadOutput(process.StandardOutput);
        var errors = ReadOutput(process.StandardError);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException exception)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(output, errors);
            if (cancellationToken.IsCancellationRequested)
                throw;
            throw new TimeoutException($"{executable} exceeded its resume rendering time limit.", exception);
        }
        await Task.WhenAll(output, errors);
        if (process.ExitCode != 0)
            throw new InvalidDataException($"{executable} failed to render the resume (exit code {process.ExitCode}).");
        return await output;
    }

    private static async Task<string> ReadOutput(StreamReader reader)
    {
        const int maximumCharacters = 8192;
        var result = new StringBuilder();
        var buffer = new char[1024];
        int read;
        while ((read = await reader.ReadAsync(buffer)) > 0)
        {
            var remaining = maximumCharacters - result.Length;
            if (remaining > 0)
                result.Append(buffer, 0, Math.Min(read, remaining));
        }
        return result.ToString();
    }
}
