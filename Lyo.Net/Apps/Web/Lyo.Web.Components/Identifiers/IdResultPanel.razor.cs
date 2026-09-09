using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Identifiers;

public partial class IdResultPanel
{
    [Parameter]
    [EditorRequired]
    public List<IdEntry> Entries { get; set; } = [];

    [Parameter]
    public string Class { get; set; } = string.Empty;

    private async Task CopyAllAsync()
    {
        var text = string.Join(Environment.NewLine, Entries.Select(e => e.Value));
        await JsInterop.SendToClipboard(text);
        Snackbar.Add($"Copied {Entries.Count} ID(s) to clipboard.", Severity.Success);
    }

    private async Task CopyItemAsync(string value)
    {
        await JsInterop.SendToClipboard(value);
        Snackbar.Add("Copied to clipboard.", Severity.Success);
    }
}
