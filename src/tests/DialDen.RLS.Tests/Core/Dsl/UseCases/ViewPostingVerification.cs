using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewPostingVerification : ResponseVerification<ViewPostingResponse>, IPostingStatusVerification, IPostingDeliveryVerification
{
    public ViewPostingVerification(ViewPostingResponse response, UseCaseContext context)
        : base(response, context)
    {
    }

    public ViewPostingVerification HasId(int expectedId)
    {
        Response.Id.ShouldBe(expectedId);
        return this;
    }

    public ViewPostingVerification HasTitle(string expectedTitle)
    {
        Response.Title.ShouldBe(expectedTitle);
        return this;
    }

    public void HasStatus(string expectedStatus) => Response.Status.ToString().ShouldBe(expectedStatus);

    public void HasDelivery(string jobSiteId, string expectedStatus)
    {
        Response.Deliveries.ShouldHaveSingleItem().ShouldBe(new PostingDeliveryResponse(jobSiteId, expectedStatus));
    }

    public void HasNoDeliveries() => Response.Deliveries.ShouldBeEmpty();
}
