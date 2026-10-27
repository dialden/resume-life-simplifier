using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ListPostings(IRLSDriver driver, UseCaseContext context)
    : RLSUseCase<ListPostingsResponse, ListPostingsVerification>(driver, context)
{
    private int? _pageNumber = 1;

    public ListPostings OnPage(int pageNumber)
    {
        _pageNumber = pageNumber;
        return this;
    }

    public ListPostings AcrossAllPages()
    {
        _pageNumber = null;
        return this;
    }

    public override async Task<RLSUseCaseResult<ListPostingsResponse, ListPostingsVerification>> Execute()
    {
        if (_pageNumber is { } requestedPage)
        {
            var result = await Driver.ListPostings(requestedPage);
            return Wrap(result.Map(page => new ListPostingsResponse([page])));
        }

        var pages = new List<PostingPageResponse>();
        var pageNumber = 1;
        while (true)
        {
            var result = await Driver.ListPostings(pageNumber);
            if (result.IsFailure)
            {
                return Wrap(Result<ListPostingsResponse, SystemError>.Failure(result.Error));
            }

            var page = result.Value;
            pages.Add(page);
            if (page.PageSize <= 0 || page.Items.Count == 0 || (long)pageNumber * page.PageSize >= page.TotalCount)
            {
                break;
            }
            pageNumber++;
        }

        return Wrap(Result<ListPostingsResponse, SystemError>.Success(new ListPostingsResponse(pages)));
    }

    private RLSUseCaseResult<ListPostingsResponse, ListPostingsVerification> Wrap(
        Result<ListPostingsResponse, SystemError> result)
        => new(result, Context, (response, ctx) => new ListPostingsVerification(response, ctx));
}
