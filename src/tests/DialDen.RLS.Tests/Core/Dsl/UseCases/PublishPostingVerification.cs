using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public sealed class PublishPostingVerification(
    PublishPostingResponse response,
    UseCaseContext context)
    : ResponseVerification<PublishPostingResponse>(response, context), IPostingStatusVerification, IJobSitePublicationVerification
{
    public void HasStatus(string expectedStatus)
    {
        Response.Status.ToString().ShouldBe(expectedStatus);
    }

    public void HasJobSitePublication(string jobSiteId, string expectedStatus)
    {
        Response.JobSitePublications.ShouldHaveSingleItem().ShouldBe(new JobSitePublicationResponse(jobSiteId, expectedStatus));
    }

    public void HasNoJobSitePublications() => Response.JobSitePublications.ShouldBeEmpty();
}
