using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>One label/value pair inside a <see cref="LyoRecordCard" />, standing in for a table column on narrow viewports.</summary>
public partial class LyoRecordField
{
    /// <summary>Field label, normally the column header the value came from.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Plain text value, used when <see cref="ChildContent" /> is not supplied.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Markup value: chips, links, timestamps, or buttons.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Drops the row entirely when the value is empty, keeping cards short where records are sparsely filled.</summary>
    [Parameter]
    public bool HideWhenEmpty { get; set; }

    private bool Hidden => HideWhenEmpty && ChildContent is null && string.IsNullOrWhiteSpace(Value);

    private string DisplayValue => string.IsNullOrWhiteSpace(Value) ? "—" : Value;
}
