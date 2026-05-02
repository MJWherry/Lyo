using System.Diagnostics;

namespace Lyo.Diagnostic.Logging;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record LogEnricherOptions
{
    /// <summary>When true, the full frame list is written to the log as a structured array. Can be verbose; disable in high-throughput services. Default: false.</summary>
    public bool IncludeAllFrames { get; init; } = false;

    /// <summary>When true, inner exceptions are each written as a nested structured object. Default: true.</summary>
    public bool IncludeInnerExceptions { get; init; } = true;

    /// <summary>Maximum number of inner exception levels to include. Default: 5 (protects against pathological chains).</summary>
    public int MaxInnerExceptionDepth { get; init; } = 5;

    public static LogEnricherOptions Default { get; } = new();

    public override string ToString()
        => $"AllFrames={IncludeAllFrames} InnerExceptions={IncludeInnerExceptions} MaxDepth={MaxInnerExceptionDepth}";
}