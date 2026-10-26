using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.RLS.UI;

public class UIRLSDriver : IRLSDriver
{
    private readonly UIClient _uiClient;

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

    public async Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id)
    {
        var homePage = await _uiClient.OpenHomePage();
        return await homePage.ViewPosting(id);
    }
}
