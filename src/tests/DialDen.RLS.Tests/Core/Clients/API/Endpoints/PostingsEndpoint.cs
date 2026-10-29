using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Microsoft.AspNetCore.Mvc;
using PostingStatus = DialDen.RLS.Abstractions.Postings.PostingStatus;

namespace DialDen.RLS.Tests.Core.Clients.API.Endpoints;

public class PostingsEndpoint
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public PostingsEndpoint(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<CreatePostingResponse, SystemError>> Create(string title)
    {
        using var response = await _httpClient.PostAsJsonAsync("/postings", new { Title = title });
        if (!response.IsSuccessStatusCode)
        {
            return Result<CreatePostingResponse, SystemError>.Failure(SystemError.Of(
                $"Creating a posting returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
        }

        var posting = await response.Content.ReadFromJsonAsync<CreatePostingResponse>(ResponseJsonOptions)
            ?? throw new InvalidOperationException("The create posting response was empty.");
        return Result<CreatePostingResponse, SystemError>.Success(posting);
    }

    public async Task<Result<ViewPostingResponse, SystemError>> View(int id)
    {
        using var response = await _httpClient.GetAsync($"/postings/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(
                $"Viewing posting {id} returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
        }

        var posting = await response.Content.ReadFromJsonAsync<ViewPostingResponse>(ResponseJsonOptions)
            ?? throw new InvalidOperationException("The view posting response was empty.");
        return Result<ViewPostingResponse, SystemError>.Success(posting);
    }

    public async Task<Result<PublishPostingResponse, SystemError>> Publish(int id, IReadOnlyList<string> jobSiteIds)
    {
        using var response = await _httpClient.PatchAsJsonAsync(
            $"/postings/{id}",
            new { Status = PostingStatus.Published, JobSiteIds = jobSiteIds },
            ResponseJsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>()
                    ?? throw new InvalidOperationException("The publishing conflict response was empty.");
                return Result<PublishPostingResponse, SystemError>.Failure(SystemError.Of(
                    problem.Detail ?? throw new InvalidOperationException("The publishing conflict had no explanation.")));
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()
                    ?? throw new InvalidOperationException("The publishing validation response was empty.");
                return Result<PublishPostingResponse, SystemError>.Failure(problem.MapError());
            }
            return Result<PublishPostingResponse, SystemError>.Failure(SystemError.Of(
                $"Publishing posting {id} returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
        }

        var posting = await response.Content.ReadFromJsonAsync<PublishPostingResponse>(ResponseJsonOptions)
            ?? throw new InvalidOperationException("The publish posting response was empty.");
        return Result<PublishPostingResponse, SystemError>.Success(posting);
    }

    public async Task<Result<PostingPageResponse, SystemError>> List(int pageNumber)
    {
        using var response = await _httpClient.GetAsync($"/postings?pageNumber={pageNumber}");
        if (!response.IsSuccessStatusCode)
        {
            return Result<PostingPageResponse, SystemError>.Failure(SystemError.Of(
                $"Listing postings returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
        }

        var page = await response.Content.ReadFromJsonAsync<PostingPageResponse>(ResponseJsonOptions)
            ?? throw new InvalidOperationException("The postings page response was empty.");
        return Result<PostingPageResponse, SystemError>.Success(page);
    }
}
