using Lyo.Api.Models.Common.Response;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

public partial class ReportTabularPreviewDialog
{
    /// <summary>Cards cannot virtualise, so wide reports only render this many rows on a narrow viewport.</summary>
    private const int CardRowLimit = 200;

    /// <summary>Pre-built sheet previews (title plus table).</summary>
    [Parameter]
    [EditorRequired]
    // Qualified because the enclosing Lyo namespace has a DataTable namespace, which outranks every using directive in this file.
    public IReadOnlyList<(string Title, Lyo.DataTable.Models.DataTable Table)> Sheets { get; set; } = [];

    private IReadOnlyList<(string Title, IReadOnlyList<string> Headers, IReadOnlyList<string[]> Rows)> _sheets = [];

    protected override void OnParametersSet()
    {
        if (Sheets is not { Count: > 0 }) {
            _sheets = [];
            return;
        }

        var mapped = new List<(string Title, IReadOnlyList<string> Headers, IReadOnlyList<string[]> Rows)>(Sheets.Count);
        foreach (var sheet in Sheets) {
            var (headers, rows) = ReportGridDataTableMapper.ToPreviewRows(sheet.Table);
            mapped.Add((sheet.Title, headers, rows));
        }

        _sheets = mapped;
    }
}
