using System.Diagnostics;

namespace Lyo.Diagnostic.StackTrace;

/// <summary>Describes a detected recursion pattern within a stack trace.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RecursionInfo(StackFrame Frame, int Depth, int StartIndex)
{
    public override string ToString() => $"Depth {Depth} × {Frame.ShortMethod} (start={StartIndex})";
}