using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewOverviewVerification : ResponseVerification<OverviewResponse>
{
    public ViewOverviewVerification(OverviewResponse response, UseCaseContext context)
        : base(response, context)
    {
    }

    public void HasTitle(string expectedTitle) => Response.Title.ShouldBe(expectedTitle);
}
