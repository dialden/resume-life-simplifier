using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewPosting : RLSUseCase<ViewPostingResponse, ViewPostingVerification>
{
    private int _id;
    private string? _idAlias;

    public ViewPosting(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public ViewPosting WithId(int id)
    {
        _id = id;
        _idAlias = null;
        return this;
    }

    public ViewPosting WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    public override async Task<RLSUseCaseResult<ViewPostingResponse, ViewPostingVerification>> Execute()
    {
        var id = _id;
        if (_idAlias != null &&
            !int.TryParse(Context.GetResultValue(_idAlias), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
        {
            throw new InvalidOperationException($"Posting ID alias '{_idAlias}' did not resolve to an integer ID.");
        }

        var result = await Driver.ViewPosting(id);
        return new RLSUseCaseResult<ViewPostingResponse, ViewPostingVerification>(
            result,
            Context,
            (response, ctx) => new ViewPostingVerification(response, ctx));
    }
}
