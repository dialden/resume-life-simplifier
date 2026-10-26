using Microsoft.Extensions.DependencyInjection;

namespace DialDen.RLS.Tests.Fixtures;

/// <summary>Environment where the RLS talks to stubbed external systems. Register their stub drivers here.</summary>
public class StubbedExternalSystemsTestEnvironment : TestEnvironment
{
    protected override string ConfigurationEnvironmentVariablesPrefix => "STUBBED_EXT_SYSTEMS_";
}
