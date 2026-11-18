namespace DialDen.RLS.Tests.Core.Dsl;

public class UseCaseDsl
{
    private readonly Func<Func<UseCaseDsl, Task>, Task>? _runScenario;
    private UseCaseDsl? _interaction;
    private readonly RLSDsl? _rls;
    private readonly ExternalSystemsDsl? _externalSystems;
    private bool _executed;
    public RLSDsl RLS => _interaction?.RLS ?? _rls
        ?? throw new InvalidOperationException("The scenario has not started.");
    public ExternalSystemsDsl ExternalSystems => _interaction?.ExternalSystems ?? _externalSystems
        ?? throw new InvalidOperationException("The scenario has not started.");

    public UseCaseDsl(RLSDsl rls, ExternalSystemsDsl externalSystems)
    {
        _rls = rls;
        _externalSystems = externalSystems;
    }

    internal UseCaseDsl(Func<Func<UseCaseDsl, Task>, Task> runScenario)
    {
        _runScenario = runScenario;
    }

    internal async Task ExecuteAsync(Func<Task> execute)
    {
        if (_executed)
            throw new InvalidOperationException("An interaction can only be awaited once.");
        _executed = true;

        if (_runScenario is null)
        {
            await execute();
            return;
        }

        await _runScenario(async interaction =>
        {
            _interaction = interaction;
            try
            {
                await execute();
            }
            finally
            {
                _interaction = null;
            }
        });
    }
}
