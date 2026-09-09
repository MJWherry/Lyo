namespace Lyo.Reporting.Models.Models;

/// <summary>How a custom-component parameter is filled from the report generate bag or a literal.</summary>
public enum ComponentBindingKind
{
    Param,
    Literal
}

/// <summary>Binds one Blazor <c>[Parameter]</c> to a report parameter key or a literal JSON/text value.</summary>
public sealed class ComponentBinding
{
    /// <summary><see cref="ComponentBindingKind.Param" /> reads a report param; <see cref="ComponentBindingKind.Literal" /> uses <see cref="Value" /> as-is (after interpolation).</summary>
    public ComponentBindingKind Kind { get; set; } = ComponentBindingKind.Param;

    /// <summary>Report param key when <see cref="Kind" /> is Param; literal JSON or text when Literal.</summary>
    public string? Value { get; set; }
}
