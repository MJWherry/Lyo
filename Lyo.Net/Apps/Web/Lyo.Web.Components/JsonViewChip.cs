using System.Diagnostics;

namespace Lyo.Web.Components;

/// <summary>Header chip for <see cref="JsonViewDialog{T}" /> (elapsed time, status code, path, and so on).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record JsonViewChip(string Label, Color Color = Color.Default, string? Icon = null);