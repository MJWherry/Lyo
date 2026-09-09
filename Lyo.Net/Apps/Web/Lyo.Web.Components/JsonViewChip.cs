namespace Lyo.Web.Components;

/// <summary>Header chip for <see cref="JsonViewDialog{T}" /> (elapsed time, status code, path, and so on).</summary>
public sealed record JsonViewChip(string Label, Color Color = Color.Default, string? Icon = null);
