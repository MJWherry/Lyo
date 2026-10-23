using System.Diagnostics;

namespace Lyo.Diff.Text;

/// <summary>Half-open span <c>[Start, Start + Length)</c> into a source string.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct TextToken(int Start, int Length);