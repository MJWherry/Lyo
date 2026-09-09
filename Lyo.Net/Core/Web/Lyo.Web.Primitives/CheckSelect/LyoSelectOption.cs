namespace Lyo.Web.Primitives.CheckSelect;

/// <summary>A labelled option consumed by <see cref="LyoCheckSelect{TValue}" />.</summary>
/// <typeparam name="TValue">The value type (usually an enum or string).</typeparam>
/// <param name="Value">Underlying value.</param>
/// <param name="Label">Display label shown in the dropdown list.</param>
public record LyoSelectOption<TValue>(TValue Value, string Label);