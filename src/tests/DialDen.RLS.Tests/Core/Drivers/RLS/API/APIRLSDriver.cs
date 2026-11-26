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

    public Task<Result<PublicListingResponse, SystemError>> ViewPublicListing(int id, Uri? address = null)
        => _apiClient.Postings.ViewPublicListing(id);

    public Task<Result<VoidValue, SystemError>> ApplyToPosting(
        int postingId, string name, string email, byte[] resume, string? referralJobSiteId)
        => _apiClient.Applications.Submit(postingId, name, email, resume, referralJobSiteId);

    public Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ListApplications(int postingId)
        => _apiClient.Applications.List(postingId);

    public Task<Result<VoidValue, SystemError>> InterruptLiveUpdates() =>
        throw new NotSupportedException("Simulating a browser connection interruption requires the UI channel.");

    public Task<Result<IReadOnlyList<ApplicationResponse>, SystemError>> ObserveViewedApplications(
        int postingId, Func<IReadOnlyList<ApplicationResponse>, bool> isExpected, TimeSpan timeout) =>
        _apiClient.Applications.Observe(postingId, isExpected, timeout);

    public Task<Result<byte[], SystemError>> DownloadResume(int postingId, int applicationId)
        => _apiClient.Applications.DownloadResume(postingId, applicationId);

    public Task<Result<VoidValue, SystemError>> ReviewEducation(
        int postingId, int applicationId, bool? overrideMet, string note, int version) =>
        _apiClient.Applications.ReviewEducation(postingId, applicationId, overrideMet, note, version);

    public async Task<Result<ComparisonResponse, SystemError>> CompareCandidates(
        int postingId, IReadOnlyList<int> applicationIds)
    {
        var posting = await _apiClient.Postings.View(postingId);
        if (posting.IsFailure) return Result<ComparisonResponse, SystemError>.Failure(posting.Error);
        var applications = await _apiClient.Applications.List(postingId);
        if (applications.IsFailure) return Result<ComparisonResponse, SystemError>.Failure(applications.Error);
        return Result<ComparisonResponse, SystemError>.Success(new(
            posting.Value.RequiredEducation, applications.Value.Where(item => applicationIds.Contains(item.Id))
                .Select(item => new CandidateComparison(item.Name, item.EducationAssessment?.Evidence)).ToArray()));
    }

    public async Task<Result<VoidValue, SystemError>> GoToRLS()
    {
        return await _apiClient.Health.CheckHealth();
    }

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title, string description, string? requiredEducation)
    {
        return await _apiClient.Postings.Create(title, description, requiredEducation);
    }

    public Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id, IReadOnlyList<string> jobSiteIds)
        => _apiClient.Postings.Publish(id, jobSiteIds);

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        return await _apiClient.Postings.View(id);
    }

    public Task<Result<ViewPostingResponse, SystemError>> ObserveViewedPosting(
        int id, Func<ViewPostingResponse, bool> isExpected, TimeSpan timeout) =>
        _apiClient.Postings.Observe(id, isExpected, timeout);

    public Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber)
        => _apiClient.Postings.List(pageNumber);
}
