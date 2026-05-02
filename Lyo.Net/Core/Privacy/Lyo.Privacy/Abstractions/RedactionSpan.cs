using System.Diagnostics;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Abstractions;

/// <summary>Inclusive start index and length in the original UTF-16 string.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly record struct RedactionSpan(int Start, int Length, RedactionKind Kind)
{
    public int End => Start + Length;

    private string DebuggerDisplay => $"{Kind} [{Start}..{End}) len={Length}";

    /// <inheritdoc />
    public override string ToString() => $"{Kind}@[{Start}:{Length}]";
}