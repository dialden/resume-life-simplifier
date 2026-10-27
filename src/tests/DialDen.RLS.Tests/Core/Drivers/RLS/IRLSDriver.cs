using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Drivers.RLS;

public interface IRLSDriver
{
    Task<Result<VoidValue, SystemError>> GoToRLS();
    Task<Result<CreatePostingResponse, SystemError>> CreatePosting(string title);
    Task<Result<PublishPostingResponse, SystemError>> PublishPosting(int id);
    Task<Result<ViewPostingResponse, SystemError>> ViewPosting(int id);
    Task<Result<PostingPageResponse, SystemError>> ListPostings(int pageNumber);
}
