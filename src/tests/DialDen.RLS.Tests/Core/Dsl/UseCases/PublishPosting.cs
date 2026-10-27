using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class PublishPosting : RLSUseCase<PublishPostingResponse, PublishPostingVerification>
{
    private string? _idAlias;

    public PublishPosting(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public PublishPosting WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    public override async Task<RLSUseCaseResult<PublishPostingResponse, PublishPostingVerification>> Execute()
    {
        var id = _idAlias is not null &&
                 int.TryParse(Context.GetResultValue(_idAlias), NumberStyles.Integer, CultureInfo.InvariantCulture, out var resolvedId)
            ? resolvedId
            : throw new InvalidOperationException($"Posting ID alias '{_idAlias}' did not resolve to an integer ID.");
        var result = await Driver.PublishPosting(id);
        return new RLSUseCaseResult<PublishPostingResponse, PublishPostingVerification>(
            result,
            Context,
            (response, ctx) => new PublishPostingVerification(response, ctx));
    }
}
