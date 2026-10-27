using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class PublishPostingVerification(
    PublishPostingResponse response,
    UseCaseContext context)
    : ResponseVerification<PublishPostingResponse>(response, context), IPostingStatusVerification
{
    public void HasStatus(string expectedStatus)
    {
        Response.Status.ToString().ShouldBe(expectedStatus);
    }
}
