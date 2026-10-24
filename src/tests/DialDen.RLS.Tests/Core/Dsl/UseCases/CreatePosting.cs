using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class CreatePosting : RLSUseCase<CreatePostingResponse, CreatePostingVerification>
{
    private string _title = "Software Engineer";

    public CreatePosting(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public CreatePosting WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public override async Task<RLSUseCaseResult<CreatePostingResponse, CreatePostingVerification>> Execute()
    {
        var result = await Driver.CreatePosting(_title);
        return new RLSUseCaseResult<CreatePostingResponse, CreatePostingVerification>(
            result,
            Context,
            (response, ctx) => new CreatePostingVerification(response, ctx));
    }
}
