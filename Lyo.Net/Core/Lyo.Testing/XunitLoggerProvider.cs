using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lyo.Testing;

public class XunitLoggerProvider(ITestOutputHelper output) : ILoggerProvider
{
    private readonly ITestOutputHelper _output = ArgumentHelpers.ThrowIfNullReturn(output, nameof(output));

    public ILogger CreateLogger(string categoryName) => new XunitLogger(_output, categoryName);

    public void Dispose()
    {
        // Nothing to dispose
    }

    private class XunitLogger(ITestOutputHelper output, string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try {
                var message = formatter(state, exception);
                var logLevelString = logLevel switch {
                    LogLevel.Trace => "TRACE",
                    LogLevel.Debug => "DEBUG",
                    LogLevel.Information => "INFO",
                    LogLevel.Warning => "WARN",
                    LogLevel.Error => "ERROR",
                    LogLevel.Critical => "CRITICAL",
                    LogLevel.None => "NONE",
                    var _ => logLevel.ToString().ToUpper()
                };

                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] [{logLevelString}] [{categoryName}] {message}";
                if (exception != null)
                    logMessage += $"\n{exception}";

                output.WriteLine(logMessage);
            }
            catch {
                // Ignore errors when writing to test output
            }
        }
    }
}