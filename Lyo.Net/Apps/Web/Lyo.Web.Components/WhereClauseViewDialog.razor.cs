using System.Text.Json;
using Lyo.Query.Models.Common;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components;

public partial class WhereClauseViewDialog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private int _activeTab;

    private string _readableText = string.Empty;

    private string _jsonText = string.Empty;

    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string? ElementId { get; set; }

    [Parameter]
    public WhereClause? WhereClause { get; set; }

    protected override void OnParametersSet()
    {
        if (WhereClause == null) {
            _readableText = string.Empty;
            _jsonText = string.Empty;
            return;
        }

        _readableText = WhereClause.Print();
        _jsonText = JsonSerializer.Serialize(WhereClause, JsonOptions);
    }

    private async Task CopyToClipboard() => await Js.SendToClipboard(_activeTab == 1 ? _jsonText : _readableText);

    private void Close() => MudDialog.Close();
}
