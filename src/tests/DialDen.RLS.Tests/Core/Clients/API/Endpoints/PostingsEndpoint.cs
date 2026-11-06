using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
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

    public async Task<Result<CreatePostingResponse, SystemError>> Create(string title, string description)
    {
        using var response = await _httpClient.PostAsJsonAsync("/postings", new { Title = title, Description = description });
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>()
                    ?? throw new InvalidOperationException("The creation validation response was empty.");
                return Result<CreatePostingResponse, SystemError>.Failure(problem.MapError());
            }
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

    public async Task<Result<PublicListingResponse, SystemError>> ViewPublicListing(int id)
    {
        using var response = await _httpClient.GetAsync($"/public-postings/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<PublicListingResponse, SystemError>.Failure(SystemError.Of("Public listing not found."));
        }
        if (!response.IsSuccessStatusCode)
        {
            return Result<PublicListingResponse, SystemError>.Failure(SystemError.Of(
                $"Viewing public listing {id} returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
        }

        var listing = await response.Content.ReadFromJsonAsync<PublicListingResponse>(ResponseJsonOptions)
            ?? throw new InvalidOperationException("The public listing response was empty.");
        return Result<PublicListingResponse, SystemError>.Success(listing);
    }

    /// <summary>
    /// Follows the posting's event stream, viewing the posting whenever an event says to, until
    /// <paramref name="isExpected"/> holds or <paramref name="timeout"/> elapses. Returns the latest view.
    /// </summary>
    public async Task<Result<ViewPostingResponse, SystemError>> Observe(
        int id, Func<ViewPostingResponse, bool> isExpected, TimeSpan timeout)
    {
        using var observing = new CancellationTokenSource(timeout);
        Result<ViewPostingResponse, SystemError>? latest = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/notifications/stream");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, observing.Token);
            if (!response.IsSuccessStatusCode)
            {
                return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(
                    $"Observing posting {id} returned HTTP {(int)response.StatusCode} ({response.StatusCode})."));
            }
            if (response.Content.Headers.ContentType?.MediaType != "text/event-stream")
            {
                return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(
                    $"Observing posting {id} returned {response.Content.Headers.ContentType} instead of an event stream."));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(observing.Token);
            await foreach (var item in SseParser.Create(stream).EnumerateAsync(observing.Token))
            {
                if (item.EventType is not ("ready" or "notifications-changed"))
                {
                    continue;
                }
                latest = await View(id);
                if (latest.IsFailure || isExpected(latest.Value))
                {
                    return latest;
                }
            }
            return Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(
                $"The event stream of posting {id} ended while it was being observed."));
        }
        catch (OperationCanceledException) when (observing.IsCancellationRequested)
        {
            return latest ?? Result<ViewPostingResponse, SystemError>.Failure(SystemError.Of(
                $"The event stream of posting {id} was not ready within {timeout}."));
        }
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
