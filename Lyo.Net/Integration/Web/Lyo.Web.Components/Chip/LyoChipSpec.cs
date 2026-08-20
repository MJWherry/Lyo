namespace Lyo.Web.Components;

/// <summary>Display chip label, color, icon, and variant for <see cref="LyoChip" />.</summary>
public sealed record LyoChipSpec(string Label, Color Color = Color.Default, string? Icon = null, Variant Variant = Variant.Filled);
