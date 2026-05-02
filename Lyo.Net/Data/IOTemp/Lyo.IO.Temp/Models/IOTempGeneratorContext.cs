using Lyo.IO.Temp.Storage;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.IO.Temp.Models;

/// <summary>
/// Wires an <see cref="IOTempFileGenerator" /> to its owning session without exposing session internals publicly. All members are delegates sourced from
/// <see cref="IOTempSession" /> at construction time.
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
    IIOTempStorageProvider Storage);