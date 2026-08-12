using Lyo.DataTable.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using LyoDataTable = Lyo.DataTable.Models.DataTable;

namespace Lyo.Web.Components.DataTablePreview;

/// <summary>Blazor host for the JS-virtualized <c>DataTable</c> preview grid.</summary>
public partial class DataTablePreview : IAsyncDisposable
{
    private const string ModuleUrl = "./_content/Lyo.Web.Components/scripts/lyoDataTablePreview.js";
    private const float RowItemSize = 32f;

    private static int _nextInstanceId;
    private readonly int _instanceId = Interlocked.Increment(ref _nextInstanceId);

    private DotNetObjectReference<DataTablePreview>? _dotNetRef;
    private ElementReference _hostRef;
    private bool _moduleLoaded;
    private IJSObjectReference? _module;
    private LyoDataTable? _mountedTable;
    private bool _needsMount;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    /// <summary>Table to preview. Large tables are row-virtualized in the browser.</summary>
    [Parameter]
    public LyoDataTable? Table { get; set; }

    /// <summary>CSS max-height for the scroll host (default <c>480px</c>).</summary>
    [Parameter]
    public string MaxHeightCss { get; set; } = "480px";

    /// <summary>When true (default), shows a sticky 1-based row number column on the left.</summary>
    [Parameter]
    public bool ShowRowNumbers { get; set; } = true;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeSessionAsync().ConfigureAwait(false);
        if (_module is not null) {
            try {
                await _module.DisposeAsync().ConfigureAwait(false);
            }
            catch {
                // ignored
            }

            _module = null;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
        _moduleLoaded = false;
    }

    /// <summary>Metadata for the JS virtualizer (headers + dimensions).</summary>
    [JSInvokable]
    public Task<DataTablePreviewMeta> GetMetaAsync()
    {
        if (Table == null) {
            return Task.FromResult(new DataTablePreviewMeta {
                TotalRows = 0,
                ColumnCount = 0,
                Headers = []
            });
        }

        var maxCol = Table.MaxColumn;
        if (maxCol < 0) {
            return Task.FromResult(new DataTablePreviewMeta {
                TotalRows = Table.Rows.Count,
                ColumnCount = 0,
                Headers = []
            });
        }

        return Task.FromResult(new DataTablePreviewMeta {
            TotalRows = Table.Rows.Count,
            ColumnCount = maxCol + 1,
            Headers = BuildHeaderRow(Table, maxCol)
        });
    }

    /// <summary>Returns a rectangular window of cell display values for rows <paramref name="start"/>..<paramref name="start"/>+count.</summary>
    [JSInvokable]
    public Task<DataTablePreviewWindow> GetWindowAsync(int start, int count)
    {
        if (Table == null || count <= 0 || start >= Table.Rows.Count) {
            return Task.FromResult(new DataTablePreviewWindow {
                Start = start,
                Cells = []
            });
        }

        var maxCol = Table.MaxColumn;
        start = Math.Max(0, start);
        count = Math.Min(count, Table.Rows.Count - start);
        var cells = new string[count][];
        for (var i = 0; i < count; i++)
            cells[i] = maxCol < 0 ? [] : BuildBodyRow(Table.Rows[start + i], maxCol);

        return Task.FromResult(new DataTablePreviewWindow {
            Start = start,
            Cells = cells
        });
    }

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Table, _mountedTable))
            _needsMount = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            try {
                _dotNetRef = DotNetObjectReference.Create(this);
                _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModuleUrl);
                _moduleLoaded = true;
                _needsMount = true;
            }
            catch {
                _moduleLoaded = false;
            }
        }

        if (!_moduleLoaded || !_needsMount)
            return;

        _needsMount = false;
        await MountOrRefreshAsync().ConfigureAwait(false);
    }

    private async Task MountOrRefreshAsync()
    {
        if (_module is null || _dotNetRef is null)
            return;

        await DisposeSessionAsync().ConfigureAwait(false);
        _mountedTable = Table;
        if (Table == null)
            return;

        // Host element only exists when Table is non-null (rendered this pass).
        await _module.InvokeVoidAsync(
            "mount",
            _hostRef,
            _dotNetRef,
            new {
                maxHeightCss = MaxHeightCss,
                showRowNumbers = ShowRowNumbers,
                rowHeight = RowItemSize
            }).ConfigureAwait(false);
    }

    private async Task DisposeSessionAsync()
    {
        if (_module is not null) {
            try {
                await _module.InvokeVoidAsync("dispose", _hostRef).ConfigureAwait(false);
            }
            catch {
                // ignored
            }
        }

        _mountedTable = null;
    }

    private static string[] BuildHeaderRow(LyoDataTable table, int maxCol)
    {
        var headers = new string[maxCol + 1];
        var covered = new HashSet<int>();
        for (var col = 0; col <= maxCol; col++) {
            if (covered.Contains(col)) {
                headers[col] = string.Empty;
                continue;
            }

            var cell = table.Headers.TryGetValue(col, out var h) ? h : DataTableCell.Empty;
            var colSpan = ClampSpan(cell.ColSpan, maxCol - col + 1);
            headers[col] = cell.DisplayValue;
            for (var k = col + 1; k < col + colSpan; k++) {
                headers[k] = string.Empty;
                covered.Add(k);
            }
        }

        return headers;
    }

    private static string[] BuildBodyRow(DataTableRow row, int maxCol)
    {
        var cells = new string[maxCol + 1];
        var covered = new HashSet<int>();
        for (var col = 0; col <= maxCol; col++) {
            if (covered.Contains(col)) {
                cells[col] = string.Empty;
                continue;
            }

            var cell = row[col];
            var colSpan = ClampSpan(cell.ColSpan, maxCol - col + 1);
            cells[col] = cell.DisplayValue;
            for (var k = col + 1; k < col + colSpan; k++) {
                cells[k] = string.Empty;
                covered.Add(k);
            }
        }

        return cells;
    }

    private static int ClampSpan(int span, int max) => span < 1 ? 1 : Math.Min(span, max);

    /// <summary>JS meta payload.</summary>
    public sealed class DataTablePreviewMeta
    {
        public int TotalRows { get; set; }
        public int ColumnCount { get; set; }
        public string[] Headers { get; set; } = [];
    }

    /// <summary>JS window payload.</summary>
    public sealed class DataTablePreviewWindow
    {
        public int Start { get; set; }
        public string[][] Cells { get; set; } = [];
    }
}
