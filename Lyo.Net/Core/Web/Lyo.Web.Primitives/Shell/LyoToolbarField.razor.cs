using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Wraps a toolbar input so dense outlined fields lose MudBlazor's extra top margin. Does not redeclare Mud field parameters; put the input in
/// <see cref="ChildContent" />. Set <see cref="Grow" /> so the field takes leftover width.
/// </summary>
/// <remarks>
/// Do not wrap a search cluster that positions its own nav buttons over the input; keep that markup as a custom group child.
/// </remarks>
public partial class LyoToolbarField
{
    /// <summary>The input, usually a dense outlined <c>MudTextField</c> or <c>MudSelect</c>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Takes leftover width on the row.</summary>
    [Parameter]
    public bool Grow { get; set; }

    /// <summary>CSS class forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>Attributes forwarded onto the wrapper.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string FieldCssClass => LyoToolbarLayout.Field(Grow);
}
