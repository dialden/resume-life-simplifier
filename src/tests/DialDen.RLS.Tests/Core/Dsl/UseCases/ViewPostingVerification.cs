using DialDen.RLS.Tests.Core.Drivers.RLS.Dtos;
using Shouldly;

namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public class ViewPostingVerification : ResponseVerification<ViewPostingResponse>, IPostingStatusVerification, IJobSitePublicationVerification
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

    public void HasDescription(string description) => Response.Description.ShouldBe(description);

    public void HasJobSitePublication(string jobSiteId, string expectedStatus)
    {
        Response.JobSitePublications.Where(publication => publication.JobSiteId == jobSiteId)
            .ShouldHaveSingleItem().ShouldBe(new JobSitePublicationResponse(jobSiteId, expectedStatus));
    }

    public void HasJobSitePublicationCount(int count) => Response.JobSitePublications.Count.ShouldBe(count);

    public void HasNoJobSitePublications() => Response.JobSitePublications.ShouldBeEmpty();
}
