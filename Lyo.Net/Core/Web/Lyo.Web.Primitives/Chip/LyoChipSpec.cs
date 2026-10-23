using System.Diagnostics;

namespace Lyo.Web.Primitives;

/// <summary>Display chip label, color, icon, variant, and optional inline style consumed by <see cref="LyoChip" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoChipSpec(string Label, Color Color = Color.Default, string? Icon = null, Variant Variant = Variant.Filled, string? Style = null);