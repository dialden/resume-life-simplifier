namespace DialDen.RLS.Tests.Core.Dsl.Scenario.Assume;

public class AssumeStage
{
    private readonly UseCaseDsl _app;

    public AssumeStage(UseCaseDsl app)
    {
        _app = app;
    }

    public AssumeRunningAction RLS()
    {
        return new AssumeRunningAction(async () =>
        {
            (await _app.RLS.GoToRLS().Execute()).ShouldSucceed();
        }, this);
    }
}

public class AssumeRunningAction
{
    private readonly Func<Task> _action;
    private readonly AssumeStage _assumeStage;

    internal AssumeRunningAction(Func<Task> action, AssumeStage assumeStage)
    {
        _action = action;
        _assumeStage = assumeStage;
    }

    public async Task<AssumeStage> ShouldBeRunning()
    {
        await _action();
        return _assumeStage;
    }
}
