using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.LyoType;

public partial class LyoTypeChip
{
    /// <summary>Stored CLR FullName (or a legacy alias).</summary>
    [Parameter]
    public string? TypeName { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Filled;

    [Parameter]
    public Size Size { get; set; } = Size.Small;

    private string LabelText => LyoTypeUi.DisplayName(TypeName);

    private string Tooltip => string.IsNullOrWhiteSpace(TypeName) ? "" : TypeName!;
}
