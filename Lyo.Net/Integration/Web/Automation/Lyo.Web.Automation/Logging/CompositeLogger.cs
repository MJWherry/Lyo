using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Logging;

/// <summary>Fans log calls out to two <see cref="ILogger" /> instances. Used to write to both the injected application logger and the per-session file logger simultaneously.</summary>
public sealed class CompositeLogger<T> : ILogger<T>
{
    private readonly ILogger _primary;
    private readonly ILogger _secondary;

    public CompositeLogger(ILogger primary, ILogger secondary)
    {
        _primary = primary;
        _secondary = secondary;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => new CompositeScope(_primary.BeginScope(state), _secondary.BeginScope(state));

    public bool IsEnabled(LogLevel logLevel) => _primary.IsEnabled(logLevel) || _secondary.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (_primary.IsEnabled(logLevel))
            _primary.Log(logLevel, eventId, state, exception, formatter);

        if (_secondary.IsEnabled(logLevel))
            _secondary.Log(logLevel, eventId, state, exception, formatter);
    }

    private sealed class CompositeScope : IDisposable
    {
        private readonly IDisposable? _a;
        private readonly IDisposable? _b;

        public CompositeScope(IDisposable? a, IDisposable? b)
        {
            _a = a;
            _b = b;
        }

        public void Dispose()
        {
            _a?.Dispose();
            _b?.Dispose();
        }
    }
}