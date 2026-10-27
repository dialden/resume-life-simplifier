namespace DialDen.RLS.Tests.Core.Dsl.UseCases;

public interface IPostingStatusVerification
{
    void HasStatus(string expectedStatus);
}
