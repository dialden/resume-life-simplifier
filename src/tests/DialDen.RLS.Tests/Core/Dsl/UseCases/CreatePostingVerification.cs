using System.Globalization;
using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class CreatePostingVerification : ResponseVerification<CreatePostingResponse>, IPostingStatusVerification
{
    public CreatePostingVerification(CreatePostingResponse response, UseCaseContext context)
        : base(response, context)
    {
    }

    public int? Id => Response.Id;

    public CreatePostingVerification SaveIdAs(string alias)
    {
        var id = Response.Id
            ?? throw new InvalidOperationException("The CreatePosting result did not include a posting ID.");
        Context.SetResultEntry(alias, id.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    public CreatePostingVerification HasTitle(string expectedTitle)
    {
        Response.Title.ShouldBe(expectedTitle);
        return this;
    }

    public void HasStatus(string expectedStatus)
    {
        Response.Status.ToString().ShouldBe(expectedStatus);
    }
}
