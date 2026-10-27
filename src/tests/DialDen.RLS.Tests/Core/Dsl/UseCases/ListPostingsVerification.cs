using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class ListPostingsVerification(ListPostingsResponse response, UseCaseContext context)
    : ResponseVerification<ListPostingsResponse>(response, context)
{
    public void IncludesPostings(string alias, int count)
    {
        var all = Response.Pages.SelectMany(page => page.Items).ToArray();
        for (var index = 1; index <= count; index++)
        {
            var id = int.Parse(Context.GetResultValue($"{alias}-{index}")!, CultureInfo.InvariantCulture);
            all.ShouldContain(posting => posting.Id == id);
        }
    }

    public void HasEmptyPage(int pageNumber)
    {
        var page = Response.Pages.ShouldHaveSingleItem();
        page.PageNumber.ShouldBe(pageNumber);
        page.PageSize.ShouldBe(10);
        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBeGreaterThan(0);
    }

    public void HasPostingStatus(string alias, string expectedStatus)
    {
        var id = int.Parse(Context.GetResultValue(alias)!, CultureInfo.InvariantCulture);
        var all = Response.Pages.SelectMany(page => page.Items).ToArray();
        all.ShouldContain(posting => posting.Id == id);
        var posting = all.Single(posting => posting.Id == id);
        posting.Status.ToString().ShouldBe(expectedStatus);
    }
}
