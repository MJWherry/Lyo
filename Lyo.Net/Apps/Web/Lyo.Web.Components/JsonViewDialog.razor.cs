using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Common.Json;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components;

public partial class JsonViewDialog<T>
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = default!;

    /// <summary>Optional HTML <c>id</c> override for the root node.</summary>
    [Parameter]
    public string? ElementId { get; set; }

    /// <summary>Object serialized and displayed as a JSON tree.</summary>
    [Parameter]
    public T Data { get; set; } = default!;

    /// <summary>Dialog title. Falls back to the title passed to <c>ShowAsync</c>.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Request path shown as a chip in the header (for example <c>Person/QueryConcrete</c>).</summary>
    [Parameter]
    public string? Path { get; set; }

    /// <summary>Optional status chips (elapsed time, HTTP status, row counts) displayed after the title.</summary>
    [Parameter]
    public IReadOnlyList<JsonViewChip>? Chips { get; set; }

    private JsonNode? rootNode;
    private string? parseError;
    private string? jsonString;

    private string ResolvedTitle => !string.IsNullOrWhiteSpace(Title) ? Title : MudDialog.Title ?? "JSON Viewer";

    protected override void OnParametersSet()
    {
        try {
            parseError = null;
            if (Data == null) {
                rootNode = null;
                return;
            }

            jsonString = JsonSerializer.Serialize(Data, LyoJsonSerializerOptions.Create(static o => o.WriteIndented = true));
            rootNode = JsonNode.Parse(jsonString);
        }
        catch (Exception ex) {
            parseError = ex.Message;
            rootNode = null;
        }
    }

    private async Task CopyToClipboard() => await Js.SendToClipboard(jsonString ?? "");
}
