using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Desktop.Components;

/// <summary>Lines for a <see cref="LogView" />. <see cref="Write(string?)" /> may run on any thread. The buffer keeps the newest <see cref="Capacity" /> lines.</summary>
public sealed class LogSink
{
    /// <summary>Newest lines kept when a caller uses <see cref="LogSink()" />.</summary>
    public const int DefaultCapacity = 10_000;

    private readonly Lock _gate = new();
    private readonly List<LogLine> _lines = [];

    /// <summary>Creates a sink that drops the oldest line once <paramref name="capacity" /> is exceeded.</summary>
    public LogSink(int capacity = DefaultCapacity)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(capacity, 1);
        Capacity = capacity;
    }

    /// <summary>Newest lines kept. Older lines are dropped.</summary>
    public int Capacity { get; }

    /// <summary>A non-blank line was added.</summary>
    public event Action<LogLine>? Written;

    /// <summary><see cref="Clear" /> removed every buffered line.</summary>
    public event Action? Cleared;

    /// <summary>Records <paramref name="message" /> at <see cref="LogLevel.Information" /> when it has text.</summary>
    public void Write(string? message) => Write(LogLevel.Information, message, null);

    /// <summary>
    /// Records <paramref name="message" /> when it has text. <paramref name="source" /> is the logger or component name. <paramref name="context" /> is scope and message
    /// properties. <paramref name="exceptionText" /> is <see cref="Exception.ToString" />.
    /// </summary>
    public void Write(LogLevel level, string? message, string? source = null, IReadOnlyList<LogField>? context = null, string? exceptionText = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        var fields = context is { Count: > 0 } ? context : null;
        var failure = string.IsNullOrWhiteSpace(exceptionText) ? null : exceptionText;
        var line = new LogLine(DateTimeOffset.UtcNow, level, message.Trim(), string.IsNullOrWhiteSpace(source) ? null : source.Trim(), fields, failure);
        lock (_gate) {
            _lines.Add(line);
            while (_lines.Count > Capacity)
                _lines.RemoveAt(0);
        }

        var written = Written;
        written?.Invoke(line);
    }

    /// <summary>A copy of the buffered lines, oldest first.</summary>
    public IReadOnlyList<LogLine> Snapshot()
    {
        lock (_gate)
            return _lines.ToArray();
    }

    /// <summary>Drops every buffered line and raises <see cref="Cleared" />. Does not touch a log file a host writes on its own.</summary>
    public void Clear()
    {
        lock (_gate)
            _lines.Clear();
        var cleared = Cleared;
        cleared?.Invoke();
    }
}

/// <summary>
/// Forwards <see cref="ILogger" /> messages into a <see cref="LogSink" />. Levels below <see cref="MinimumLevel" /> are dropped. Scopes and the exception are kept on the line.
/// </summary>
public sealed class LogSinkLoggerProvider(LogSink sink) : ILoggerProvider, ISupportExternalScope
{
    private readonly LogSink _sink = ArgumentHelpers.ThrowIfNullReturn(sink);
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        ArgumentHelpers.ThrowIfNull(scopeProvider);
        _scopes = scopeProvider;
    }

    /// <summary>Lowest level forwarded to the sink. Defaults to <see cref="LogLevel.Information" />.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new SinkLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose() { }

    private sealed class SinkLogger(LogSinkLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => provider._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= provider.MinimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var text = formatter(state, exception);
            if (exception != null && (string.IsNullOrWhiteSpace(text) || !text.Contains(exception.Message, StringComparison.Ordinal)))
                text = string.IsNullOrWhiteSpace(text) ? exception.Message : text + " " + exception.Message;
            var fields = new List<LogField>();
            provider._scopes.ForEachScope(static (scope, list) => Collect(scope, list), fields);
            Collect(state, fields);
            provider._sink.Write(logLevel, text, category, fields, exception?.ToString());
        }

        private static void Collect(object? state, List<LogField> fields)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
                return;
            foreach (var pair in pairs) {
                if (pair.Key == "{OriginalFormat}" || string.IsNullOrWhiteSpace(pair.Key))
                    continue;
                var value = pair.Value?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                var field = new LogField(pair.Key, value);
                var index = fields.FindIndex(item => item.Name == pair.Key);
                if (index >= 0)
                    fields[index] = field;
                else
                    fields.Add(field);
            }
        }
    }
}

/// <summary>An <see cref="ILoggerFactory" /> that only uses one <see cref="LogSinkLoggerProvider" />.</summary>
public sealed class LogSinkLoggerFactory(LogSinkLoggerProvider provider) : ILoggerFactory
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => provider.CreateLogger(categoryName);

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider) { }

    /// <inheritdoc />
    public void Dispose() => provider.Dispose();
}
