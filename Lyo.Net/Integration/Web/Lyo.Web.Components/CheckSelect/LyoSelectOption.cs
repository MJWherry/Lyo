namespace Lyo.Web.Components.CheckSelect;

/// <summary>A labelled option for use with <see cref="LyoCheckSelect{TValue}"/>.</summary>
/// <typeparam name="TValue">The value type (typically an enum or string).</typeparam>
/// <param name="Value">The underlying value.</param>
/// <param name="Label">Display label shown in the dropdown list.</param>
public record LyoSelectOption<TValue>(TValue Value, string Label);
