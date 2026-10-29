namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public interface IPostingDeliveryVerification
{
    void HasDelivery(string jobSiteId, string expectedStatus);

    void HasNoDeliveries();
}
