using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewPosting : RLSUseCase<ViewPostingResponse, ViewPostingVerification>
{
    private int _id;

    public ViewPosting(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public ViewPosting WithId(int id)
    {
        _id = id;
        return this;
    }

    public override async Task<RLSUseCaseResult<ViewPostingResponse, ViewPostingVerification>> Execute()
    {
        var result = await Driver.ViewPosting(_id);
        return new RLSUseCaseResult<ViewPostingResponse, ViewPostingVerification>(
            result,
            Context,
            (response, ctx) => new ViewPostingVerification(response, ctx));
    }
}
