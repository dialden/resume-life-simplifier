using Microsoft.Extensions.Configuration;

namespace DialDen.RLS.Tests.Fixtures;

public sealed class TestConfiguration
{
    public HostingConfiguration Hosting { get; set; } = new();

    public static TestConfiguration From(IConfiguration configuration)
    {
        var settings = configuration.Get<TestConfiguration>() ?? new TestConfiguration();
        settings.Hosting.Validate();
        return settings;
    }
}
