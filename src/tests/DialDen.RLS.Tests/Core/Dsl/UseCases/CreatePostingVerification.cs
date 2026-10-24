using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class CreatePostingVerification : ResponseVerification<CreatePostingResponse>
{
    public CreatePostingVerification(CreatePostingResponse response, UseCaseContext context)
        : base(response, context)
    {
    }

    public int? Id => Response.Id;

    public CreatePostingVerification HasTitle(string expectedTitle)
    {
        Response.Title.ShouldBe(expectedTitle);
        return this;
    }
}
