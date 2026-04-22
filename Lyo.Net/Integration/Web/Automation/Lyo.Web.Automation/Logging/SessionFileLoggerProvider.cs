using System.Text;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Automation.Logging;

/// <summary>
/// Per-session <see cref="ILoggerProvider" /> that appends structured log lines to <c>{sessionDirectory}/session.log</c>. Create one per browser session and dispose it when
/// the session ends to flush and close the file.
/// </summary>
public sealed class SessionFileLoggerProvider : ILoggerProvider
{
    private readonly object _gate = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public SessionFileLoggerProvider(string sessionDirectory)
    {
        Directory.CreateDirectory(sessionDirectory);
        var logPath = Path.Combine(sessionDirectory, "session.log");
        _writer = new(new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.None), new UTF8Encoding(false)) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) => new SessionFileLogger(categoryName, _writer, _gate);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        lock (_gate)
            _writer.Dispose();
    }
}