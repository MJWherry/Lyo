using Lyo.Api.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Web.Components.LyoType;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Parameter editor shared by job definitions, job schedules, and report definitions. Hosts own persistence; this component edits <see cref="LyoParameterEditRow" />
/// in place and reports every change through <see cref="OnChanged" />.
/// </summary>
/// <remarks>
/// Rows draw either as a dense table or as one card each, see <see cref="LyoParameterLayout" />. The host sets the starting layout through
/// <c>AddLyoParameterEditor</c>, and the user's own choice is remembered in local storage from then on.
/// </remarks>
public partial class LyoParameterEditor : IDisposable
{
    /// <summary>Heading shown above the editor (for example "Job Parameters", "Schedule Parameters").</summary>
    [Parameter]
    public string Title { get; set; } = "Parameters";

    /// <summary>Typography for <see cref="Title" />. Definitions use <see cref="Typo.h6" />; reports can pass <see cref="Typo.subtitle2" />.</summary>
    [Parameter]
    public Typo TitleTypo { get; set; } = Typo.h6;

    /// <summary>Rows edited in place; the parent owns the list and persists it with its own Save action.</summary>
    [Parameter]
    [EditorRequired]
    public List<LyoParameterEditRow> Items { get; set; } = [];

    /// <summary>Show Required on the Options column (definition parameters). Marks the param as required at run time only.</summary>
    [Parameter]
    public bool ShowRequired { get; set; }

    /// <summary>Show Encrypt on the Options column (definition parameters). Marks the value for encryption at rest.</summary>
    [Parameter]
    public bool ShowEncrypt { get; set; }

    /// <summary>Show Enabled on the Options column (schedule parameters).</summary>
    [Parameter]
    public bool ShowEnabled { get; set; }

    /// <summary>Show Options kind in the expansion panel (definition parameters).</summary>
    [Parameter]
    public bool ShowOptionsEditor { get; set; }

    /// <summary>
    /// Offer the Literal / Expression choice for the default (definition parameters). Expression defaults keep the declared <see cref="LyoParameterEditRow.Type" /> and author
    /// the default as a template instead, so a <c>DateTime</c> parameter can default to yesterday. Off for schedule and trigger overrides, which supply values rather than
    /// declare defaults.
    /// </summary>
    [Parameter]
    public bool ShowDefaultKind { get; set; }

    /// <summary>If true, newly added rows start with <see cref="LyoParameterEditRow.Required" /> set.</summary>
    [Parameter]
    public bool DefaultRequired { get; set; }

    /// <summary>Caption under the default-value editor. Null uses a generic "callers supply the value" hint.</summary>
    [Parameter]
    public string? DefaultValueHint { get; set; }

    /// <summary>Required when rendering Options-backed Value selects (definition editor).</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>
    /// Parameters whose Options / AllowedValues are copied onto a row when its Key matches. If set, the Key cell is a select of those keys so schedule
    /// overrides pick up the same Static / root-query picker.
    /// </summary>
    [Parameter]
    public IReadOnlyList<LyoParameterEditRow>? InheritFrom { get; set; }

    /// <summary>Fired after any edit, add, or delete so the parent can mark itself dirty.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>Show reorder affordances (drag handle, # column, move up/down) so definition parameters can be reordered. Off for schedule overrides.</summary>
    [Parameter]
    public bool AllowReorder { get; set; }

    /// <summary>
    /// Sample data for autocomplete and live preview on formatter-typed parameters. Hosts pass a dictionary or DTO whose paths mirror what a template resolves against
    /// at run time, so the user does not have to remember the token names.
    /// </summary>
    [Parameter]
    public object? FormatterContext { get; set; }

    /// <summary>Starting layout. Overrides the host default from <c>AddLyoParameterEditor</c>, but not a layout the user picked earlier.</summary>
    [Parameter]
    public LyoParameterLayout? InitialLayout { get; set; }

    /// <summary>Resolved with <c>GetService</c> so hosts that never call <c>AddLyoParameterEditor</c> or register <see cref="ClientStore" /> still work.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private readonly HashSet<LyoParameterEditRow> _expanded = [];

    private readonly LyoViewportWatcher _viewport = new();

    private LyoParameterLayout? _layout;
    private LyoParameterLayout? _storedLayout;

    private int? _dragSourceIndex;
    private int? _dragOverIndex;

    private LyoParameterEditorOptions EditorOptions => Services.GetService<LyoParameterEditorOptions>() ?? new LyoParameterEditorOptions();

    private LyoDataGridOptions GridOptions => Services.GetService<LyoDataGridOptions>() ?? new LyoDataGridOptions();

    /// <summary>A layout picked from the toggle wins; otherwise narrow viewports get cards because the table does not fit, then the stored and host defaults apply.</summary>
    private LyoParameterLayout Layout
        => _layout
           ?? (_viewport.IsAtOrBelow(GridOptions.CardBreakpoint) ? LyoParameterLayout.Cards : (LyoParameterLayout?)null)
           ?? _storedLayout
           ?? InitialLayout
           ?? EditorOptions.Layout;

    private bool ShowLayoutToggle => EditorOptions.AllowLayoutToggle && Items.Count > 0;

    private int ColSpan => 1 + (AllowReorder ? 2 : 0) + 4;

    private Dictionary<string, string?> SiblingMap => Items.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);

    private string ResolvedDefaultHint => DefaultValueHint ?? "Optional definition default. Leave empty so callers supply the value.";

    public void Dispose() => _ = _viewport.DisposeAsync();

    /// <summary>Local storage and the viewport are unavailable during prerender, so both are read on the first interactive draw.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await _viewport.StartAsync(Services.GetService<IBrowserViewportService>(), () => InvokeAsync(StateHasChanged));

        if (Services.GetService<ClientStore>() is not { } store)
            return;

        LyoParameterLayout? saved;
        try {
            saved = await store.GetParameterLayoutAsync();
        }
        catch (Exception) {
            return;
        }

        if (saved is null || saved == Layout)
            return;

        _storedLayout = saved;
        StateHasChanged();
    }

    private bool IsExpanded(LyoParameterEditRow row) => _expanded.Contains(row);

    private bool IsDropTarget(int index) => AllowReorder && _dragOverIndex == index && _dragSourceIndex is int d && d != index;

    private bool IsDragging(int index) => AllowReorder && _dragSourceIndex == index;

    private async Task SetLayout(LyoParameterLayout layout)
    {
        _layout = layout;
        if (Services.GetService<ClientStore>() is { } store)
            await store.SetParameterLayoutAsync(layout);
    }

    private void ToggleExpanded(LyoParameterEditRow row)
    {
        if (!_expanded.Add(row))
            _expanded.Remove(row);
    }

    private void ExpandIfNeeded(LyoParameterEditRow row)
    {
        if (LyoTypeUi.NeedsConcreteFullName(row.Type) || LyoTypeUi.IsWideEditor(row.Type))
            _expanded.Add(row);
    }

    private Task SetType(LyoParameterEditRow row, string type)
        => Set(() => {
            if (string.Equals(row.Type, type, StringComparison.Ordinal))
                return;

            var previous = row.Type;
            row.Type = type;
            if (LyoTypeUi.ShouldResetValue(previous, type))
                row.Value = null;
            ExpandIfNeeded(row);
        });

    private async Task Set(Action apply)
    {
        apply();
        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync();
    }

    private Task SetEncrypt(LyoParameterEditRow row, bool encrypt)
        => Set(() => {
            row.IsEncrypted = encrypt;
            if (!encrypt)
                row.EncryptedValue = null;
            else
                row.EncryptedValue ??= [];
        });

    private Task SetKey(LyoParameterEditRow row, string? key)
        => Set(() => {
            row.Key = key ?? "";
            ApplyInherit(row);
        });

    private void ApplyInherit(LyoParameterEditRow row)
    {
        var src = InheritFrom?.FirstOrDefault(p => string.Equals(p.Key, row.Key, StringComparison.OrdinalIgnoreCase));
        if (src is null)
            return;

        row.Type = src.Type;
        row.Options = src.Options;
        row.AllowedValues = src.AllowedValues;
        if (string.IsNullOrWhiteSpace(row.Description))
            row.Description = src.Description;
        ExpandIfNeeded(row);
    }

    private async Task AddRow()
    {
        var used = Items.Select(i => i.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var next = InheritFrom?.FirstOrDefault(p => !used.Contains(p.Key));
        var row = new LyoParameterEditRow {
            IsNew = true,
            Key = next?.Key ?? $"Param{Items.Count + 1}",
            Type = next?.Type ?? LyoTypeInfo.String.FullName,
            Required = DefaultRequired,
            Order = Items.Count == 0 ? 0 : Items.Max(r => r.Order) + 1
        };
        ApplyInherit(row);
        Items.Add(row);
        _expanded.Add(row);

        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync();
    }

    private async Task DeleteRow(LyoParameterEditRow row)
    {
        Items.Remove(row);
        _expanded.Remove(row);
        ReindexOrders();
        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync();
    }

    private void OnRowDragStart(DragEventArgs e, int index)
    {
        _dragSourceIndex = index;
        _dragOverIndex = null;
        if (e.DataTransfer != null)
            e.DataTransfer.EffectAllowed = "move";
    }

    private void OnDragEnterRow(int index)
    {
        if (_dragSourceIndex is int from && from != index)
            _dragOverIndex = index;
    }

    private void OnDragEnd()
    {
        _dragSourceIndex = null;
        _dragOverIndex = null;
    }

    private async Task OnRowDrop(int dropIndex)
    {
        if (_dragSourceIndex is not int fromIdx)
            return;

        OnDragEnd();
        var insertAt = dropIndex > fromIdx ? dropIndex - 1 : dropIndex;
        await Move(fromIdx, insertAt);
    }

    /// <summary>Moves a row to a new index and renumbers <see cref="LyoParameterEditRow.Order" />. Out-of-range or no-op moves are ignored.</summary>
    private async Task Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Items.Count || toIndex < 0 || toIndex >= Items.Count || fromIndex == toIndex)
            return;

        var item = Items[fromIndex];
        Items.RemoveAt(fromIndex);
        Items.Insert(toIndex, item);
        ReindexOrders();
        if (OnChanged.HasDelegate)
            await OnChanged.InvokeAsync();
    }

    private void ReindexOrders()
    {
        for (var i = 0; i < Items.Count; i++)
            Items[i].Order = i;
    }
}
