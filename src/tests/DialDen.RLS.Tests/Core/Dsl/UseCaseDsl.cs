namespace DialDen.RLS.Tests.Core.Dsl;

public class UseCaseDsl
{
    public RLSDsl RLS { get; }
    public ExternalSystemsDsl ExternalSystems { get; }

    public UseCaseDsl(RLSDsl rls, ExternalSystemsDsl externalSystems)
    {
        RLS = rls;
        ExternalSystems = externalSystems;
    }
}
