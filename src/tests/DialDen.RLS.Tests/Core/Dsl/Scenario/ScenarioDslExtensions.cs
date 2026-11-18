namespace DialDen.RLS.Tests.Core.Dsl.Scenario;

public static class ScenarioDslExtensions
{
    public static ScenarioDsl Scenario(this UseCaseDsl interaction) => new(interaction);
}
