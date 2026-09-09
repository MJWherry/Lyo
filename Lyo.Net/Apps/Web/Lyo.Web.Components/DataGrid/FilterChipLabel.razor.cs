using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

public partial class FilterChipLabel
{
    [Parameter]
    public string? ElementId { get; set; }

    [Parameter]
    [EditorRequired]
    public string Text { get; set; } = default!;

    /// <summary>Full filter description for the view dialog. If null, <see cref="Text" /> is used.</summary>
    [Parameter]
    public string? DialogText { get; set; }

    [Parameter]
    public int MaxLength { get; set; } = ChipLabelHelper.DefaultFilterChipMaxLength;

    private bool _viewDialogOpen;

    private string DialogBodyText => string.IsNullOrEmpty(DialogText) ? Text : DialogText!;

    private readonly DialogOptions _viewDialogOptions = new() { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true };

    private void OpenViewDialog() => _viewDialogOpen = true;
}
