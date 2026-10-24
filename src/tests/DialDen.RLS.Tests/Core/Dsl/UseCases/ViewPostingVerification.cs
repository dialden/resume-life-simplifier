using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewPostingVerification : ResponseVerification<ViewPostingResponse>
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
}
