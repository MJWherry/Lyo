using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Logging;

internal sealed class SessionFileLogger(string category, StreamWriter writer, object gate) : ILogger
{
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
            return;

        var levelShort = logLevel switch {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            var _ => logLevel.ToString()
        };

        lock (gate) {
            writer.WriteLine($"{DateTime.UtcNow:O} [{levelShort}] {category}: {message}");
            if (exception != null)
                writer.WriteLine($"  Exception: {exception}");
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}