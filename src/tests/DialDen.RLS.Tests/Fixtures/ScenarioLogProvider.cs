using Microsoft.Extensions.Logging;

namespace DialDen.RLS.Tests.Fixtures;

internal sealed class ScenarioLogProvider(ScenarioDiagnostics diagnostics, string source) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ScenarioLogger(diagnostics, source, categoryName);
    public void Dispose() { }

    private sealed class ScenarioLogger(ScenarioDiagnostics diagnostics, string source, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level != LogLevel.None;

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = $"{level} {category} event={eventId.Id}";
            if (exception is not null)
                message += $" exception={exception.GetType().FullName}";
            diagnostics.Record(source, message);
        }
    }
}
