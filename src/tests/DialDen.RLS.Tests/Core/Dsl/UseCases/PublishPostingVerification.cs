using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class PublishPostingVerification(
    PublishPostingResponse response,
    UseCaseContext context)
    : ResponseVerification<PublishPostingResponse>(response, context), IPostingStatusVerification, IJobSitePublicationVerification
{
    public int Id => Response.Id;

    public Uri PublicListingUrl => Response.PublicListingUrl.ShouldNotBeNull();

    public void HasStatus(string expectedStatus)
    {
        Response.Status.ToString().ShouldBe(expectedStatus);
    }

    public void HasJobSitePublication(string jobSiteId, string expectedStatus)
    {
        Response.JobSitePublications.Where(publication => publication.JobSiteId == jobSiteId)
            .ShouldHaveSingleItem().ShouldBe(new JobSitePublicationResponse(jobSiteId, expectedStatus));
    }

    public void HasJobSitePublicationCount(int count) => Response.JobSitePublications.Count.ShouldBe(count);

    public void HasNoJobSitePublications() => Response.JobSitePublications.ShouldBeEmpty();
}
