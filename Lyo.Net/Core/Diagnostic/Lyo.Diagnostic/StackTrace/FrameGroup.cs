using System.Diagnostics;

namespace Lyo.Diagnostic.StackTrace;

/// <summary>A contiguous run of frames sharing the same <see cref="FrameCategory" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FrameGroup(FrameCategory Category, IReadOnlyList<StackFrame> Frames)
{
    public int Count => Frames.Count;

    public override string ToString() => $"{Category} {Count} frame{(Count == 1 ? "" : "s")}";
}