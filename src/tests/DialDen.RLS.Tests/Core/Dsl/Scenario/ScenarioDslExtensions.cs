using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.Core.Dsl.Scenario;

public static class ScenarioDslExtensions
{
    /// <summary>Starts a scenario using the default system channel.</summary>
    public static ScenarioDsl Scenario(this TestEnvironment testEnvironment)
        => new(testEnvironment.InteractWithSystem());

    /// <summary>Starts a scenario that interacts with the system through the given channel.</summary>
    public static ScenarioDsl Scenario(this TestEnvironment testEnvironment, Channel channel)
        => new(testEnvironment.InteractWithSystemThrough(channel));
}
