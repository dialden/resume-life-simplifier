namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public interface IJobSitePublicationVerification
{
    void HasJobSitePublication(string jobSiteId, string expectedStatus);

    void HasNoJobSitePublications();
}
