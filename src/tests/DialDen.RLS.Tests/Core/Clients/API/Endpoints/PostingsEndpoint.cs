using System.Net.Http.Json;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Clients.API.Endpoints;

public class PostingsEndpoint
{
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

        var posting = await response.Content.ReadFromJsonAsync<CreatePostingResponse>()
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

        var posting = await response.Content.ReadFromJsonAsync<ViewPostingResponse>()
            ?? throw new InvalidOperationException("The view posting response was empty.");
        return Result<ViewPostingResponse, SystemError>.Success(posting);
    }
}
