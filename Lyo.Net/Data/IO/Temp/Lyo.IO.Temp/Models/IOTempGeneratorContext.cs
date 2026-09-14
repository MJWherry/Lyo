using Lyo.IO.FileSystem;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.IO.Temp.Models;

/// <summary>
/// Connects <see cref="IOTempFileGenerator" /> to its session without leaking session internals. Members are delegates taken from <see cref="IOTempSession" /> at construction.
/// </summary>
internal sealed record IOTempGeneratorContext(
    string SessionDirectory,
    Action ThrowIfDisposed,
    Func<string?, bool, string> ResolvePath,
    Func<string, string> EnsureWithinSession,
    Action<long> ValidateSize,
    Action<string, long> RegisterFile,
    Action<string> RegisterDirectory,
    IOTempSessionOptions Options,
    ILogger Logger,
    IMetrics Metrics,
    IFileSystem Storage);