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
            if (state is IReadOnlyList<KeyValuePair<string, object?>> values &&
                TryFormatTiming(category, eventId.Id, values, out var timing))
            {
                diagnostics.Record(source, timing);
                return;
            }
            var message = $"{level} {category} event={eventId.Id}";
            if (exception is not null)
                message += $" exception={exception.GetType().FullName}";
            diagnostics.Record(source, message);
        }

        private static bool TryFormatTiming(string category, int eventId,
            IReadOnlyList<KeyValuePair<string, object?>> values, out string timing)
        {
            timing = "";
            if (eventId is < 1001 or > 1003)
                return false;
            object? Value(string key) => values.FirstOrDefault(pair => pair.Key == key).Value;
            var phase = eventId switch { 1001 => "started", 1002 => "completed", _ => "faulted" };
            if (category == "DialDen.RLS.Infrastructure.Applications.ResumePageImageRenderer" &&
                Value("Process") is string process && process is "libreoffice" or "pdfinfo" or "pdftoppm" &&
                Value("Operation") is Guid operation)
                timing = $"process={process} phase={phase}";
            else if (category == "DialDen.RLS.Worker.ExtractApplicationInformationConsumer" &&
                     Value("Attempt") is int attempt && attempt >= 0)
                timing = $"extraction attempt={attempt} phase={phase}";
            else
                return false;

            if (eventId != 1001 && Value("ElapsedMs") is long elapsed && elapsed >= 0)
                timing += $" elapsedMs={elapsed}";
            if (Value("Operation") is Guid id)
                timing += $" operation={id}";
            if (Value("MessageId") is Guid messageId)
                timing += $" message={messageId}";
            if (Value("ExceptionType") is string type &&
                type.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '+' or '`'))
                timing += $" exception={type}";
            return true;
        }
    }
}
