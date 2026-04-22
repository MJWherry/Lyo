using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Logging;

internal sealed class SessionFileLogger : ILogger
{
    private readonly string _category;
    private readonly StreamWriter _writer;
    private readonly object _gate;

    public SessionFileLogger(string category, StreamWriter writer, object gate)
    {
        _category = category;
        _writer = writer;
        _gate = gate;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
            return;

        var levelShort = logLevel switch {
            LogLevel.Trace       => "TRC",
            LogLevel.Debug       => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning     => "WRN",
            LogLevel.Error       => "ERR",
            LogLevel.Critical    => "CRT",
            _                    => logLevel.ToString()
        };

        lock (_gate) {
            _writer.WriteLine($"{DateTime.UtcNow:O} [{levelShort}] {_category}: {message}");
            if (exception != null)
                _writer.WriteLine($"  Exception: {exception}");
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
