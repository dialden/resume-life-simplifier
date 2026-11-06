using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ViewPublicListing : RLSUseCase<PublicListingResponse, PublicListingVerification>
{
    private int _id;
    private string? _idAlias;
    private Uri? _address;

    public ViewPublicListing(IRLSDriver driver, UseCaseContext context) : base(driver, context)
    {
    }

    public ViewPublicListing WithId(int id)
    {
        _id = id;
        _idAlias = null;
        return this;
    }

    public ViewPublicListing WithId(string idAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idAlias);
        _idAlias = idAlias;
        return this;
    }

    public ViewPublicListing AtAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        _address = address;
        return this;
    }

    public override async Task<RLSUseCaseResult<PublicListingResponse, PublicListingVerification>> Execute()
    {
        var id = _id;
        if (_idAlias is not null &&
            !int.TryParse(Context.GetResultValue(_idAlias), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
        {
            throw new InvalidOperationException($"Posting ID alias '{_idAlias}' did not resolve to an integer ID.");
        }

        var result = await Driver.ViewPublicListing(id, _address);
        return new RLSUseCaseResult<PublicListingResponse, PublicListingVerification>(
            result, Context,
            (response, context) => new PublicListingVerification(response, context, id));
    }
}
