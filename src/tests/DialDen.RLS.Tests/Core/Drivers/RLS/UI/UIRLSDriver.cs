using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Clients.UI.Pages;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.RLS.UI;

public class UIRLSDriver : IRLSDriver
{
    private readonly UIClient _uiClient;
    private PostingsPage? _postingsPage;

    public UIRLSDriver(UIClient uiClient)
    {
        _uiClient = uiClient;
    }

    public async Task<Result<VoidValue, SystemError>> GoToRLS()
    {
        var homePage = await _uiClient.OpenHomePage();
        var error = await homePage.GetError();
        return error == null
            ? Result.Success<SystemError>()
            : Result.Failure<SystemError>(SystemError.Of($"The RLS UI displayed an error: {error}"));
    }

    public async Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title)
    {
        var homePage = await _uiClient.OpenHomePage();
        return await homePage.CreatePosting(title);
    }

    public async Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id)
    {
        var homePage = await _uiClient.OpenHomePage();
        var postingsPage = await homePage.OpenPostings();
        if (postingsPage.IsFailure)
        {
            return Result<PublishPostingResponse, SystemError>.Failure(postingsPage.Error);
        }

        return await postingsPage.Value.PublishPosting(id);
    }

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        var homePage = await _uiClient.OpenHomePage();
        return await homePage.ViewPosting(id);
    }

    public async Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber)
    {
        if (_postingsPage is null)
        {
            var homePage = await _uiClient.OpenHomePage();
            var result = await homePage.OpenPostings();
            if (result.IsFailure)
            {
                return Result<PostingPageResponse, SystemError>.Failure(result.Error);
            }
            _postingsPage = result.Value;
        }
        return await _postingsPage.ReadPage(pageNumber);
    }
}
