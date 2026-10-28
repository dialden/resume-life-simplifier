using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
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

    public async Task<Result<PublishPostingResponse, SystemError>> Publish(int id)
    {
        using var response = await _httpClient.PatchAsJsonAsync(
            $"/postings/{id}",
            new { Status = PostingStatus.Published },
            ResponseJsonOptions);
        if (!response.IsSuccessStatusCode)
        {
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
