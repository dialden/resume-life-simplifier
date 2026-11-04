using DialDen.RLS.Tests.Core.Clients.API;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.RLS.API;

public class APIRLSDriver : IRLSDriver
{
    private readonly APIClient _apiClient;

    public APIRLSDriver(APIClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Result<VoidValue, SystemError>> GoToRLS()
    {
        return await _apiClient.Health.CheckHealth();
    }

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title, string description)
    {
        return await _apiClient.Postings.Create(title, description);
    }

    public Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id, IReadOnlyList<string> jobSiteIds)
        => _apiClient.Postings.Publish(id, jobSiteIds);

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        return await _apiClient.Postings.View(id);
    }

    public Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber)
        => _apiClient.Postings.List(pageNumber);

    public Task<Result<VoidValue, SystemError>> ChooseLanguage(Language language)
    {
        _apiClient.AcceptLanguage(language == Language.French ? "fr" : "en");
        return Task.FromResult(Result.Success<SystemError>());
    }

    public Task<Result<OverviewResponse, SystemError>> ViewOverview()
        => Task.FromResult(Result<OverviewResponse, SystemError>.Failure(
            SystemError.Of("The API has no overview to view.")));
}
