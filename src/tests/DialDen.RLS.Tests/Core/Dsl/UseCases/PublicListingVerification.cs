using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class PublicListingVerification(
    PublicListingResponse response,
    UseCaseContext context,
    int expectedId) : ResponseVerification<PublicListingResponse>(response, context)
{
    public int Id => Response.Id;

    public Uri Address => Response.PublicListingUrl.ShouldNotBeNull();

    public void HasTitle(string title) => Response.Title.ShouldBe(title);

    public void HasDescription(string description) => Response.Description.ShouldBe(description);

    public void HasShareableAddress()
    {
        Response.Id.ShouldBe(expectedId);
        Address.IsAbsoluteUri.ShouldBeTrue();
        Address.Scheme.ShouldBeOneOf("http", "https");
        Address.AbsolutePath.ShouldBe($"/jobs/{expectedId}");
        Address.Query.ShouldBeEmpty();
        Address.Fragment.ShouldBeEmpty();
    }

    public void Matches(PublicListingVerification other) => Response.ShouldBe(other.Response);
}
