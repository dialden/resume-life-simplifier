using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Environment where the RLS talks to the real external systems. Register their drivers here.</summary>
public class RealExternalSystemsTestEnvironment : TestEnvironment
{
    protected override string ConfigurationEnvironmentVariablesPrefix => "REAL_EXT_SYSTEMS_";

}
