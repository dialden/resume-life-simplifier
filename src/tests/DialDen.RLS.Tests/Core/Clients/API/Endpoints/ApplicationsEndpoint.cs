using System.Net.Http.Headers;
using System.Net.Http.Json;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Clients.API.Endpoints;

public sealed class ApplicationsEndpoint(HttpClient httpClient)
{
    public async Task<Result<VoidValue, SystemError>> Submit(
        int postingId, string name, string email, byte[] resume, string? referralJobSiteId)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent(email), "email");
        if (referralJobSiteId is not null)
            content.Add(new StringContent(referralJobSiteId), "referralJobSiteId");
        var file = new ByteArrayContent(resume);
        var isWord = resume.AsSpan().StartsWith("PK\x03\x04"u8);
        file.Headers.ContentType = new MediaTypeHeaderValue(isWord
            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/pdf");
        content.Add(file, "resume", isWord ? "resume.docx" : "resume.pdf");
        using var response = await httpClient.PostAsync($"/public-postings/{postingId}/applications", content);
        return response.IsSuccessStatusCode
            ? Result.Success<SystemError>()
            : Result.Failure<SystemError>(SystemError.Of(
                $"Submitting an application returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
    }

    public async Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> List(int postingId)
    {
        using var response = await httpClient.GetAsync($"/postings/{postingId}/applications");
        if (!response.IsSuccessStatusCode)
        {
            return Result<IReadOnlyList<ApplicationResponse>, SystemError>.Failure(SystemError.Of(
                $"Listing applications returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
        }

        var applications = await response.Content.ReadFromJsonAsync<ApplicationResponse[]>()
            ?? throw new InvalidOperationException("The applications response was empty.");
        return Result<IReadOnlyList<ApplicationResponse>, SystemError>.Success(applications);
    }

    public async Task<Result<byte[], SystemError>> DownloadResume(int postingId, int applicationId)
    {
        using var response = await httpClient.GetAsync(
            $"/postings/{postingId}/applications/{applicationId}/resume");
        return response.IsSuccessStatusCode
            ? Result<byte[], SystemError>.Success(await response.Content.ReadAsByteArrayAsync())
            : Result<byte[], SystemError>.Failure(SystemError.Of(
                $"Downloading a resume returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
    }
}
