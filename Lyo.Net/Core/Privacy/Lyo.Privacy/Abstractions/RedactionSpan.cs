using System.Diagnostics;
using Lyo.Privacy.Enums;

namespace Lyo.Privacy.Abstractions;

/// <summary>Inclusive start index and length in the original UTF-16 string.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct RedactionSpan(int Start, int Length, RedactionKind Kind)
{
    public int End => Start + Length;

    /// <inheritdoc />
    public override string ToString() => $"{Kind}@[{Start}:{Length}]";
}