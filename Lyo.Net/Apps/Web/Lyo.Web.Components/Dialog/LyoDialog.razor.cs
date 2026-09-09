using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Dialog;

public partial class LyoDialog
{
    [CascadingParameter]
    IMudDialogInstance MudDialogInstance { get; set; } = null!;

    [Inject]
    IDialogService DialogService { get; set; } = null!;

    /// <summary>Optional title rendered inside the dialog header. When null (and <see cref="TitleContent" /> is null), the title passed to <c>ShowAsync</c> is used.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Custom header markup; takes precedence over <see cref="Title" />. Title row only. put chips in <see cref="TitleChips" />.</summary>
    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    /// <summary>Status chips rendered as a wrap row under the title header (above the body) so they do not collide with the debug / close buttons.</summary>
    [Parameter]
    public RenderFragment? TitleChips { get; set; }

    /// <summary>
    /// Payload displayed by the header debug button as pretty-printed JSON (typically the API response or row that opened this popup).
    /// The button is always rendered; it is disabled when this is null.
    /// </summary>
    [Parameter]
    public object? DebugData { get; set; }

    /// <summary>Dialog body. Rendered inside a scrollable region capped to the viewport height so long content never pushes the actions off-screen.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Extra buttons rendered before the Close button (for example Re-Run, Cancel Run).</summary>
    [Parameter]
    public RenderFragment? ExtraActions { get; set; }

    /// <summary>Additional inline style applied to the scrollable content region.</summary>
    [Parameter]
    public string? ContentStyle { get; set; }

    /// <summary>If set, a primary Save button is rendered that invokes this callback. The dialog is not closed automatically; close it from the callback on success.</summary>
    [Parameter]
    public EventCallback OnSave { get; set; }

    /// <summary>Disables the Save button (for example while the form has no changes).</summary>
    [Parameter]
    public bool SaveDisabled { get; set; }

    /// <summary>Displays a progress indicator and disables the action buttons while a save/load is in flight.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Label of the dismiss button. Default is "Close".</summary>
    [Parameter]
    public string CloseText { get; set; } = "Close";

    /// <summary>Label of the primary button. Default is "Save".</summary>
    [Parameter]
    public string SaveText { get; set; } = "Save";

    private async Task ShowDebugJson()
    {
        if (DebugData == null)
            return;

        var parameters = new DialogParameters<JsonViewDialog<object>> { { i => i.Data, DebugData } };
        await DialogService.ShowAsync(typeof(JsonViewDialog<object>), "Raw response JSON", parameters, LyoDialogPresets.Medium);
    }

    private void Close() => MudDialogInstance.Cancel();
}
