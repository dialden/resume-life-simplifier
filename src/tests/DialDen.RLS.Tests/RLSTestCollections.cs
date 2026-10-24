using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests;

[CollectionDefinition(Name)]
public class RLSTestCollection : ICollectionFixture<TestEnvironments>
{
    public const string Name = nameof(RLSTestCollection);
}
