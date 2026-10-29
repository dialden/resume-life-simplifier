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

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title)
    {
        return await _apiClient.Postings.Create(title);
    }

    public Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id, IReadOnlyList<string> jobSiteIds)
        => _apiClient.Postings.Publish(id, jobSiteIds);

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        return await _apiClient.Postings.View(id);
    }

    public Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber)
        => _apiClient.Postings.List(pageNumber);
}
