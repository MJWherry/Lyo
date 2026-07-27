namespace Lyo.Web.Components.Dialog;

/// <summary>
/// Standard <see cref="DialogOptions" /> presets so every popup in the app opens with a consistent size and behavior.
/// Use these instead of constructing <see cref="DialogOptions" /> inline when calling <c>IDialogService.ShowAsync</c>.
/// </summary>
public static class LyoDialogPresets
{
    /// <summary>Compact dialog sized to its content (confirmations, small pickers). Max width <see cref="MaxWidth.Small" />.</summary>
    public static DialogOptions Small => new() {
        MaxWidth = MaxWidth.Small,
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    /// <summary>Default dialog for single-entity forms and viewers. Fixed at <see cref="MaxWidth.Medium" /> width.</summary>
    public static DialogOptions Medium => new() {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    /// <summary>Wide dialog for tabbed/master-detail editors (e.g. job or report definitions). Fixed at <see cref="MaxWidth.Large" /> width.</summary>
    public static DialogOptions Large => new() {
        MaxWidth = MaxWidth.Large,
        FullWidth = true,
        CloseButton = true,
        CloseOnEscapeKey = true
    };
}
