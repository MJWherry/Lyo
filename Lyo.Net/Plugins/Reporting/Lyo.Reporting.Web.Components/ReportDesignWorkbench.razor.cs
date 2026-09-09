using System.Globalization;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Metadata.Records;
using Lyo.Parameters;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Parameters;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Sanitization;
using Lyo.Reporting.Web;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.ParamTable;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Block = Lyo.Reporting.Models.Controls.Block;

namespace Lyo.Reporting.Web.Components;

/// <summary>Visual composition designer: structure, parameter schema, example values, live HTML preview, and optional generate-via-API.</summary>
public partial class ReportDesignWorkbench : IDisposable
{
    private enum DesignTarget
    {
        Report,
        Section,
        Card,
        Block,
        Table,
        Grid,
        TableColumn,
        TableRow
    }

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Reporting";

    [Parameter]
    public Guid? DefinitionId { get; set; }

    [Parameter]
    public Func<Guid, string?, CancellationToken, Task>? DownloadFileAsync { get; set; }

    [Parameter]
    public Func<Guid, CancellationToken, Task<string?>>? ViewFileUrlAsync { get; set; }

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private string DefinitionRoute => $"{BaseRoute.TrimEnd('/')}/Definition";

    private string GenerateRoute => $"{BaseRoute.TrimEnd('/')}/Generation/Generate";

    private Report<object> _report = ReportDesignTemplates.Blank();
    private List<LyoParameterEditRow> _paramRows = [];
    private List<LyoParameterEntry> _exampleEntries = [];
    private DesignTarget _target = DesignTarget.Report;
    private Section? _section;
    private Control? _control;
    private Card? _card;
    private Block? _block;
    private Table? _table;
    private Grid? _grid;
    private TableColumn? _tableColumn;
    private TableRow? _tableRow;
    private Guid? _loadedDefinitionId;
    private string _definitionName = "Untitled report";
    private string? _definitionDescription;
    private ReportFormat _generateFormat = ReportFormat.Html;
    private bool _busy;
    private bool _loaded;
    private int _editorTab;
    private string? _previewError;
    private string _jsonDraft = "{}";
    private IReadOnlyList<ReportDefinitionRes> _definitions = [];
    private List<DesignBodyDrop> _bodyDrops = [];
    private MudDropContainer<DesignBodyDrop>? _dropContainer;
    private readonly HashSet<string> _expanded = new(StringComparer.Ordinal);
    private bool _fromCanvas;
    private string? _pendingScrollPane;
    private string? _pendingScrollKey;
    private IJSObjectReference? _designJs;
    private IDisposable? _navHandler;
    private readonly List<string> _undo = [];
    private readonly List<string> _redo = [];
    private string _snapshot = "{}";
    private bool _dirty;
    private bool _restoring;

    private sealed class DesignBodyDrop
    {
        public string ZoneId { get; set; } = "";
        public Section Section { get; set; } = null!;
        public SectionBodyItem Item { get; set; } = null!;
    }

    private Layout Layout => _report.Layout ??= new();

    private static readonly ContentType[] BlockTypes =
        Enum.GetValues<ContentType>().OrderBy(t => t.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();

    protected override void OnInitialized()
        => _navHandler = Navigation.RegisterLocationChangingHandler(OnLocationChanging);

    public void Dispose() => _navHandler?.Dispose();

    protected override async Task OnParametersSetAsync()
    {
        if (_loaded)
            return;

        _loaded = true;
        await ReloadDefinitionListAsync();
        if (DefinitionId is { } id)
            await LoadDefinitionAsync(id, confirmDiscard: false);
        else {
            ApplyTemplate(ReportDesignTemplates.ControlsGallery(), "Controls gallery");
            SelectReport();
            ResetHistory();
        }
    }

    private IReadOnlyDictionary<string, string?> PreviewParameterMap()
    {
        try {
            SyncSpecsFromRows();
            var examples = LyoParameterEntry.SiblingMap(_exampleEntries);
            _previewError = null;
            return ReportCompositionProcessor.ToMap(_report.ParameterSpecs, examples);
        }
        catch (Exception ex) {
            _previewError = ex.Message;
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void ApplyTemplate(Report<object> report, string? name = null)
    {
        _report = report;
        _report.Layout ??= new();
        _loadedDefinitionId = null;
        if (!string.IsNullOrWhiteSpace(name))
            _definitionName = name!;

        LoadRowsFromSpecs();
        SelectReport();
        SeedExpandedDefaults();
        RefreshJsonDraft();
    }

    private async Task ApplyTemplateAsync(Report<object> report, string name)
    {
        if (!await ConfirmDiscardAsync())
            return;

        ApplyTemplate(report, name);
        ResetHistory();
    }

    private void LoadRowsFromSpecs()
    {
        _paramRows = _report.ParameterSpecs.Select(
                s => new LyoParameterEditRow {
                    Key = s.Key,
                    Type = string.IsNullOrWhiteSpace(s.Type) ? LyoTypeInfo.String.FullName : s.Type,
                    Value = s.DefaultValue,
                    Description = s.Description,
                    Required = s.Required,
                    AllowedValues = s.AllowedValues,
                    IsNew = true
                })
            .ToList();

        RebuildExampleEntries(preserveByKey: _report.ParameterSpecs.ToDictionary(s => s.Key, s => s.ExampleValue ?? s.DefaultValue, StringComparer.OrdinalIgnoreCase));
    }

    private void LoadRowsFromDefinition(ReportDefinitionRes def)
    {
        if (_report.ParameterSpecs.Count == 0 && def.Parameters is { Count: > 0 }) {
            _report.ParameterSpecs = def.Parameters.Select(
                    p => new ParameterSpec {
                        Key = p.Key,
                        Type = p.Type,
                        Description = p.Description,
                        Required = p.Required,
                        DefaultValue = p.Value,
                        ExampleValue = p.Value,
                        AllowedValues = p.AllowedValues
                    })
                .ToList();
        }

        if (def.Parameters is { Count: > 0 }) {
            _paramRows = def.Parameters.Select(
                    p => LyoParameterEditRow.FromMasked(
                        p.Id, p.Key, p.Type, p.Value, p.Description, p.Required, p.EncryptedValue, p.AllowedValues, p.Options, defaultKind: p.DefaultKind,
                        defaultTemplate: p.DefaultTemplate))
                .ToList();
            var examples = _report.ParameterSpecs.ToDictionary(s => s.Key, s => s.ExampleValue ?? s.DefaultValue, StringComparer.OrdinalIgnoreCase);
            foreach (var p in def.Parameters)
                examples.TryAdd(p.Key, p.Value);

            RebuildExampleEntries(examples);
            return;
        }

        LoadRowsFromSpecs();
    }

    private void SyncSpecsFromRows()
    {
        var examples = _exampleEntries.ToDictionary(e => e.Definition.Key, e => e.SubmitValue, StringComparer.OrdinalIgnoreCase);
        _report.ParameterSpecs = _paramRows.Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .Select(
                r => new ParameterSpec {
                    Key = r.Key.Trim(),
                    Type = r.Type,
                    Description = r.Description,
                    Required = r.Required,
                    DefaultValue = r.ValueForSave,
                    ExampleValue = examples.GetValueOrDefault(r.Key) ?? r.ValueForSave,
                    AllowedValues = r.AllowedValues
                })
            .ToList();
    }

    private void OnSchemaChanged()
    {
        var previous = _exampleEntries.ToDictionary(e => e.Definition.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);
        RebuildExampleEntries(previous);
        AfterStructureChanged();
    }

    private void RebuildExampleEntries(IReadOnlyDictionary<string, string?>? preserveByKey)
    {
        var drafts = _paramRows.Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .Select(
                r => {
                    var draft = new DraftParam {
                        Key = r.Key.Trim(),
                        Type = r.Type,
                        Description = r.Description,
                        Required = r.Required,
                        AllowedValues = r.AllowedValues,
                        Options = r.Options,
                        Value = preserveByKey is not null && preserveByKey.TryGetValue(r.Key, out var kept) && kept is not null ? kept : r.Value ?? r.ValueForSave
                    };
                    return draft;
                })
            .ToList();

        _exampleEntries = LyoParameterEntry.From(drafts);
        foreach (var entry in _exampleEntries) {
            if (preserveByKey is not null && preserveByKey.TryGetValue(entry.Definition.Key, out var kept) && kept is not null)
                entry.Value = kept;
        }
    }

    private async Task ReloadDefinitionListAsync()
    {
        try {
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<ReportDefinitionRes>>(
                $"{DefinitionRoute}/QueryConcrete", new() { Amount = 200, Include = ["Parameters"] });
            _definitions = res?.Items ?? [];
        }
        catch (Exception ex) {
            Snackbar.Add($"Could not list definitions: {ex.Message}", Severity.Warning);
        }
    }

    private async Task LoadDefinitionAsync(Guid id, bool notify = true, bool confirmDiscard = true)
    {
        if (confirmDiscard && !await ConfirmDiscardAsync())
            return;

        try {
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<ReportDefinitionRes>>(
                $"{DefinitionRoute}/QueryConcrete", new() { Keys = [[id]], Amount = 1, Include = ["Parameters"] });
            var def = res?.Items?.FirstOrDefault();
            if (def is null) {
                Snackbar.Add("Definition not found.", Severity.Warning);
                return;
            }

            _report = string.IsNullOrWhiteSpace(def.ReportDataJson) ? ReportDesignTemplates.Blank() : ReportJson.Deserialize<object>(def.ReportDataJson);
            _report.Layout ??= new();
            _loadedDefinitionId = def.Id;
            _definitionName = def.Name;
            _definitionDescription = def.Description;
            LoadRowsFromDefinition(def);
            SelectReport();
            SeedExpandedDefaults();
            ResetHistory();
            if (notify)
                Snackbar.Add($"Loaded '{def.Name}'.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Load failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveDefinitionAsync()
    {
        if (string.IsNullOrWhiteSpace(_definitionName)) {
            Snackbar.Add("Name is required to save a definition.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            if (!await PersistDefinitionAsync(createNew: false))
                return;

            Snackbar.Add(_loadedDefinitionId is not null ? "Definition saved." : "Definition created.", Severity.Success);
            _dirty = false;
            await ReloadDefinitionListAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task SaveAsDefinitionAsync()
    {
        if (string.IsNullOrWhiteSpace(_definitionName)) {
            Snackbar.Add("Name is required to save a definition.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            if (!await PersistDefinitionAsync(createNew: true))
                return;

            Snackbar.Add("Saved as a new definition.", Severity.Success);
            _dirty = false;
            await ReloadDefinitionListAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    /// <summary>Writes the current composition. <paramref name="createNew" /> always inserts and points the workbench at the new id.</summary>
    private async Task<bool> PersistDefinitionAsync(bool createNew)
    {
        SyncSpecsFromRows();
        var req = new ReportDefinitionReq {
            Name = _definitionName.Trim(),
            Description = _definitionDescription,
            ReportDataJson = ReportJson.Serialize(_report),
            IsActive = true,
            DefaultFormat = _generateFormat,
            CreateParameters = createNew || _loadedDefinitionId is null ? ToCreateParameters() : []
        };

        if (!createNew && _loadedDefinitionId is { } id) {
            var updated = await ApiClient.PostAsAsync<UpdateRequest<ReportDefinitionReq>, UpdateResult<ReportDefinitionRes>>(
                $"{DefinitionRoute}/Update", new(req, id));
            if (updated.Error is not null) {
                Snackbar.Add(updated.Error.Detail ?? "Save failed.", Severity.Error);
                return false;
            }

            await SyncDefinitionParametersAsync(id);
            return true;
        }

        var created = await ApiClient.PostAsAsync<ReportDefinitionReq, CreateResult<ReportDefinitionRes>>(DefinitionRoute, req);
        if (!created.IsSuccess || created.Data is null) {
            Snackbar.Add(created.Error?.Detail ?? "Create failed.", Severity.Error);
            return false;
        }

        _loadedDefinitionId = created.Data.Id;
        await LoadDefinitionAsync(created.Data.Id, notify: false);
        return true;
    }

    private List<ReportDefinitionParameterReq> ToCreateParameters()
        => _paramRows.Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .Select(
                r => new ReportDefinitionParameterReq {
                    Key = r.Key.Trim(),
                    Type = r.Type,
                    Value = r.ValueForSave,
                    Description = r.Description,
                    Required = r.Required,
                    AllowedValues = r.AllowedValues,
                    Options = r.Options,
                    DefaultKind = r.DefaultKind,
                    DefaultTemplate = r.DefaultTemplateForSave
                })
            .ToList();

    private async Task SyncDefinitionParametersAsync(Guid definitionId)
    {
        var existing = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<ReportDefinitionRes>>(
            $"{DefinitionRoute}/QueryConcrete", new() { Keys = [[definitionId]], Amount = 1, Include = ["Parameters"] });
        var owned = existing?.Items?.FirstOrDefault()?.Parameters ?? [];
        await LyoParameterEditRowSync.SaveAsync<ReportDefinitionParameterReq, ReportDefinitionParameterRes>(
            ApiClient, $"{DefinitionRoute}/Parameter", _paramRows, owned.Select(p => p.Id), row => new ReportDefinitionParameterReq {
                ReportDefinitionId = definitionId,
                Key = row.Key,
                Type = row.Type,
                Value = row.ValueForSave,
                Description = row.Description,
                Required = row.Required,
                EncryptedValue = row.EncryptedValueForSave,
                AllowedValues = row.AllowedValues,
                Options = row.Options,
                DefaultKind = row.DefaultKind,
                DefaultTemplate = row.DefaultTemplateForSave
            }, p => p.Id);
    }

    private async Task GenerateAsync()
    {
        if (_loadedDefinitionId is null) {
            Snackbar.Add("Save as definition first so generate runs the stored composition JSON.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            if (!await PersistDefinitionAsync(createNew: false))
                return;

            _dirty = false;
            var parameters = _exampleEntries.Where(p => p.ShouldSubmit)
                .Select(p => new ReportGenerationParameterReq { Key = p.Definition.Key, Type = p.Definition.Type, Value = p.SubmitValue, Description = p.Definition.Description })
                .ToList();

            var req = new GenerateReportReq {
                ReportDefinitionId = _loadedDefinitionId,
                Format = _generateFormat,
                Parameters = parameters,
                IncludeReportData = true
            };

            var result = await ApiClient.PostAsAsync<GenerateReportReq, ReportGenerationRes>(GenerateRoute, req);
            if (result.Status == ReportGenerationStatus.Succeeded)
                Snackbar.Add($"Generated {result.Format} ({result.Id}).", Severity.Success);
            else
                Snackbar.Add($"Generation finished with status {result.Status}.", Severity.Warning);

            var dialogParams = new DialogParameters<ReportGenerationView> {
                { d => d.Generation, result },
                { d => d.DownloadFileAsync, DownloadFileAsync },
                { d => d.ViewFileUrlAsync, ViewFileUrlAsync },
                { d => d.LoadReportDataAsync, ReportGenerationDataLoader.Create(ApiClient, $"{BaseRoute.TrimEnd('/')}/Generation") },
                { d => d.OpenOutput, true }
            };
            await DialogService.ShowAsync<ReportGenerationView>("Report Generation", dialogParams, LyoDialogPresets.Medium);
        }
        catch (Exception ex) {
            Snackbar.Add($"Generate failed: {ex.Message}", Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task OnDefinitionPicked(Guid? id) => id is { } g ? LoadDefinitionAsync(g) : Task.CompletedTask;

    private async Task<bool> ConfirmDiscardAsync()
    {
        if (!_dirty)
            return true;

        return await DialogService.ConfirmAsync("Unsaved changes", "Discard unsaved report changes?", "Discard");
    }

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        if (!_dirty)
            return;

        if (!await ConfirmDiscardAsync())
            context.PreventNavigation();
    }

    private void ResetHistory()
    {
        _undo.Clear();
        _redo.Clear();
        _dirty = false;
        RefreshJsonDraft();
        _snapshot = _jsonDraft;
    }

    private void OnInspectorChanged() => AfterStructureChanged();

    private void RefreshJsonDraft()
    {
        _jsonDraft = ReportJson.Serialize(_report);
        RebuildBodyDrops();
    }

    private void RebuildBodyDrops()
    {
        _bodyDrops = [];
        foreach (var section in _report.Sections.OrderBy(s => s.Order))
            CollectDrops(section);

        _dropContainer?.Refresh();
    }

    private void CollectDrops(Section section)
    {
        var zone = ReportDesignTreeExpand.LiveKey(section);
        foreach (var item in SectionBody.Enumerate(section)) {
            _bodyDrops.Add(new() { ZoneId = zone, Section = section, Item = item });
            if (item.Kind == SectionBodyKind.Subsection && item.Subsection is not null)
                CollectDrops(item.Subsection);
        }
    }

    private void AfterStructureChanged()
    {
        if (!_restoring) {
            var next = ReportJson.Serialize(_report);
            if (!string.Equals(_snapshot, next, StringComparison.Ordinal)) {
                if (_undo.Count == 0 || !string.Equals(_undo[^1], _snapshot, StringComparison.Ordinal))
                    _undo.Add(_snapshot);

                if (_undo.Count > 40)
                    _undo.RemoveAt(0);

                _redo.Clear();
                _dirty = true;
            }
        }

        RefreshJsonDraft();
        _snapshot = _jsonDraft;
        StateHasChanged();
    }

    private bool CanRelocateSelected
        => _target is DesignTarget.Card or DesignTarget.Block or DesignTarget.Table or DesignTarget.Grid or DesignTarget.Section
            && _section is not null
            && SelectedBodyItem() is not null;

    private bool SelectedControlIsInGrid
        => _control is not null
            && _section is not null
            && SectionBody.TryOwner(_section, _control, out var owner)
            && owner is not null
            && !ReferenceEquals(owner, _section.Controls);

    private SectionBodyItem? SelectedBodyItem()
    {
        if (_section is null)
            return null;

        return _target switch {
            DesignTarget.Card or DesignTarget.Block or DesignTarget.Table or DesignTarget.Grid when _control is not null
                => new() { Kind = SectionBodyKind.Control, Section = _section, Control = _control },
            DesignTarget.Section => FindSubsectionItem(_section),
            _ => null
        };
    }

    private SectionBodyItem? FindSubsectionItem(Section section)
    {
        foreach (var parent in AllSections()) {
            foreach (var item in SectionBody.Enumerate(parent)) {
                if (item.Kind == SectionBodyKind.Subsection && ReferenceEquals(item.Subsection, section))
                    return item;
            }
        }

        return null;
    }

    private IEnumerable<Section> AllSections()
    {
        foreach (var section in _report.Sections)
            foreach (var found in Walk(section))
                yield return found;
    }

    private static IEnumerable<Section> Walk(Section section)
    {
        yield return section;
        foreach (var child in section.Subsections) {
            foreach (var found in Walk(child))
                yield return found;
        }
    }

    private IEnumerable<Section> RelocateDestinations()
    {
        var item = SelectedBodyItem();
        foreach (var dest in AllSections()) {
            if (item?.Kind == SectionBodyKind.Subsection && item.Subsection is not null && SectionBody.Contains(item.Subsection, dest))
                continue;

            if (ReferenceEquals(dest, _section) && _target is DesignTarget.Card or DesignTarget.Block or DesignTarget.Table or DesignTarget.Grid && !SelectedControlIsInGrid)
                continue;

            yield return dest;
        }
    }

    private void RelocateSelectedTo(Section dest)
    {
        var item = SelectedBodyItem();
        if (item is null || _section is null)
            return;

        if (item.Kind == SectionBodyKind.Control && item.Control is not null && SelectedControlIsInGrid) {
            if (!SectionBody.TryOwner(_section, item.Control, out var owner) || owner is null)
                return;

            owner.Remove(item.Control);
            AppendControl(dest, item.Control);
            _section = dest;
            AfterStructureChanged();
            return;
        }

        var source = item.Section;
        if (_target == DesignTarget.Section) {
            var parent = ParentOf(_section);
            if (parent is null)
                return;

            source = parent;
        }

        if (!SectionBody.Relocate(source, dest, item, SectionBody.Enumerate(dest).Count)) {
            Snackbar.Add("That move is not allowed.", Severity.Warning);
            return;
        }

        _section = dest;
        AfterStructureChanged();
    }

    private Section? ParentOf(Section section)
    {
        foreach (var candidate in AllSections()) {
            if (candidate.Subsections.Contains(section))
                return candidate;
        }

        return null;
    }

    private static Grid? FindOwnerGrid(Section section, Control control)
    {
        foreach (var grid in SectionBody.WalkControls(section.Controls).OfType<Grid>()) {
            if (grid.Controls.Contains(control))
                return grid;
        }

        return null;
    }

    private void OnBodyDropped(MudItemDropInfo<DesignBodyDrop> drop)
    {
        if (drop.Item is null)
            return;

        var dest = AllSections().FirstOrDefault(s => ReportDesignTreeExpand.LiveKey(s) == drop.DropzoneIdentifier);
        if (dest is null)
            return;

        if (!SectionBody.Relocate(drop.Item.Section, dest, drop.Item.Item, drop.IndexInZone)) {
            Snackbar.Add("That move is not allowed.", Severity.Warning);
            return;
        }

        AfterStructureChanged();
    }

    private void ApplyJsonDraft()
    {
        try {
            _report = ReportJson.Deserialize<object>(_jsonDraft);
            _report.Layout ??= new();
            LoadRowsFromSpecs();
            SelectReport();
            SeedExpandedDefaults();
            AfterStructureChanged();
            Snackbar.Add("JSON applied.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Invalid JSON: {ex.Message}", Severity.Error);
        }
    }

    private ReportDesignSelection CurrentSelection()
        => new() {
            Section = _section,
            Control = _control,
            Card = _card,
            Block = _block,
            Table = _table,
            Grid = _grid,
            TableColumn = _tableColumn,
            TableRow = _tableRow
        };

    private string CurrentDesignKey()
    {
        if (_target == DesignTarget.Report || _section is null)
            return ReportDesignTreeExpand.ReportKey;

        return _target switch {
            DesignTarget.Card when _card is not null => ReportDesignTreeExpand.CardKey(_section, _card),
            DesignTarget.Block when _block is not null => ReportDesignTreeExpand.BlockKey(_section, _block),
            DesignTarget.Table when _table is not null => ReportDesignTreeExpand.TableKey(_section, _table),
            DesignTarget.Grid when _grid is not null => ReportDesignTreeExpand.GridKey(_section, _grid),
            DesignTarget.TableColumn when _table is not null && _tableColumn is not null => ReportDesignTreeExpand.TableColumnKey(_section, _table, _tableColumn),
            DesignTarget.TableRow when _table is not null && _tableRow is not null => ReportDesignTreeExpand.TableRowKey(_section, _table, _tableRow),
            _ => ReportDesignTreeExpand.SectionKey(_section)
        };
    }

    private void QueuePeerScroll()
    {
        _pendingScrollKey = CurrentDesignKey();
        _pendingScrollPane = _fromCanvas ? ".lyo-report-design-left" : ".lyo-report-design-canvas";
    }

    private string TreeRowClass(bool selected)
        => selected ? "lyo-design-tree-row lyo-design-tree-selected" : "lyo-design-tree-row";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingScrollPane is null || string.IsNullOrEmpty(_pendingScrollKey))
            return;

        var pane = _pendingScrollPane;
        var key = _pendingScrollKey;
        _pendingScrollPane = null;
        _pendingScrollKey = null;
        try {
            _designJs ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Lyo.Reporting.Web.Components/scripts/report-design.js");
            await _designJs.InvokeVoidAsync("scrollIntoView", pane, key);
        }
        catch (JSDisconnectedException) {
        }
        catch (JSException) {
        }
    }

    private void OnCanvasHit(ReportDesignHit hit)
    {
        _fromCanvas = true;
        try {
            ApplyCanvasHit(hit);
        }
        finally {
            _fromCanvas = false;
        }
    }

    private void ApplyCanvasHit(ReportDesignHit hit)
    {
        if (hit.TableColumn is not null && hit.Section is not null && hit.Table is not null) {
            SelectTableColumn(hit.Section, hit.Table, hit.TableColumn);
            return;
        }

        if (hit.TableRow is not null && hit.Section is not null && hit.Table is not null) {
            SelectTableRow(hit.Section, hit.Table, hit.TableRow);
            return;
        }

        if (hit.Card is not null && hit.Section is not null) {
            SelectCard(hit.Section, hit.Card);
            return;
        }

        if (hit.Block is not null && hit.Section is not null) {
            SelectBlock(hit.Section, hit.Block);
            return;
        }

        if (hit.Table is not null && hit.Section is not null) {
            SelectTable(hit.Section, hit.Table);
            return;
        }

        if (hit.Grid is not null && hit.Section is not null) {
            SelectGrid(hit.Section, hit.Grid);
            return;
        }

        if (hit.Control is not null && hit.Section is not null) {
            SelectControl(hit.Section, hit.Control);
            return;
        }

        if (hit.Section is not null)
            SelectSection(hit.Section);
        else
            SelectReport();
    }

    private void OnCanvasDrop(ReportDesignDropRequest drop)
    {
        _fromCanvas = true;
        try {
            if (drop.DestGrid is not null && drop.Item.Control is not null) {
                if (!SectionBody.RelocateToGrid(drop.Source, drop.Item.Control, drop.DestGrid, drop.InsertIndex)) {
                    Snackbar.Add("That control cannot move into the grid.", Severity.Warning);
                    return;
                }

                SelectControl(drop.Dest, drop.Item.Control);
                AfterStructureChanged();
                return;
            }

            if (!SectionBody.Relocate(drop.Source, drop.Dest, drop.Item, drop.InsertIndex)) {
                Snackbar.Add("That move is not allowed.", Severity.Warning);
                return;
            }

            if (drop.Item.Kind == SectionBodyKind.Subsection && drop.Item.Subsection is not null)
                SelectSection(drop.Item.Subsection);
            else if (drop.Item.Control is not null)
                SelectControl(drop.Dest, drop.Item.Control);
            else
                SelectSection(drop.Dest);

            AfterStructureChanged();
        }
        finally {
            _fromCanvas = false;
        }
    }

    private void OnCanvasDelete(ReportDesignHit hit)
    {
        OnCanvasHit(hit);
        if (_target != DesignTarget.Report)
            DeleteSelected();
    }

    private void OnCanvasAdd(ReportDesignAddRequest request)
    {
        _fromCanvas = true;
        try {
            SelectSection(request.Section);
            if (request.Grid is not null)
                SelectGrid(request.Section, request.Grid);

            switch (request.Kind) {
            case ReportDesignAddKind.Subsection:
                AddSubsection();
                break;
            case ReportDesignAddKind.Card:
                AddCard();
                break;
            case ReportDesignAddKind.Table:
                AddTable();
                break;
            case ReportDesignAddKind.Grid:
                AddGrid();
                break;
            case ReportDesignAddKind.Block when request.BlockType is { } type:
                AddBlock(type);
                break;
            }
        }
        finally {
            _fromCanvas = false;
        }
    }

    private void SeedExpandedDefaults()
    {
        _expanded.Clear();
        foreach (var section in _report.Sections)
            _expanded.Add(ReportDesignTreeExpand.SectionKey(section));
    }

    private void ExpandToSelection()
    {
        foreach (var key in ReportDesignTreeExpand.AncestorKeys(_report.Sections, CurrentSelection()))
            _expanded.Add(key);

        if (_section is not null && _control is not null) {
            var owner = FindOwnerGrid(_section, _control);
            if (owner is not null)
                _expanded.Add(ReportDesignTreeExpand.GridKey(_section, owner));
        }

        if (_section is not null && _table is not null && _target is DesignTarget.TableColumn or DesignTarget.TableRow)
            _expanded.Add(ReportDesignTreeExpand.TableKey(_section, _table));

        RefreshTreeUi();
    }

    private void ToggleExpanded(string key)
    {
        if (!_expanded.Add(key))
            _expanded.Remove(key);

        RefreshTreeUi();
    }

    private void RefreshTreeUi() => _dropContainer?.Refresh();

    private bool IsExpanded(string key) => _expanded.Contains(key);

    private void ClearSelectionNodes()
    {
        _section = null;
        _control = null;
        _card = null;
        _block = null;
        _table = null;
        _grid = null;
        _tableColumn = null;
        _tableRow = null;
    }

    private void SelectReport()
    {
        _target = DesignTarget.Report;
        ClearSelectionNodes();
        QueuePeerScroll();
    }

    private void ApplySection(Section section)
    {
        _section = section;
        _control = null;
        _card = null;
        _block = null;
        _table = null;
        _grid = null;
        _tableColumn = null;
        _tableRow = null;
    }

    private void SelectSection(Section section)
    {
        _target = DesignTarget.Section;
        ApplySection(section);
        ExpandToSelection();
        QueuePeerScroll();
    }

    private void SelectControl(Section section, Control control)
    {
        switch (control) {
            case Card card:
                SelectCard(section, card);
                break;
            case Block block:
                SelectBlock(section, block);
                break;
            case Table table:
                SelectTable(section, table);
                break;
            case Grid grid:
                SelectGrid(section, grid);
                break;
        }
    }

    private void SelectCard(Section section, Card card)
    {
        _target = DesignTarget.Card;
        ApplySection(section);
        _control = card;
        _card = card;
        ExpandToSelection();
        QueuePeerScroll();
    }

    private void SelectBlock(Section section, Block block)
    {
        _target = DesignTarget.Block;
        ApplySection(section);
        _control = block;
        _block = block;
        ExpandToSelection();
        QueuePeerScroll();
    }

    private void SelectTable(Section section, Table table)
    {
        _target = DesignTarget.Table;
        ApplySection(section);
        _control = table;
        _table = table;
        ExpandToSelection();
        QueuePeerScroll();
    }

    private void SelectGrid(Section section, Grid grid)
    {
        _target = DesignTarget.Grid;
        ApplySection(section);
        _control = grid;
        _grid = grid;
        ExpandToSelection();
        QueuePeerScroll();
    }

    private void SelectTableColumn(Section section, Table table, TableColumn column)
    {
        _target = DesignTarget.TableColumn;
        ApplySection(section);
        _control = table;
        _table = table;
        _tableColumn = column;
        ExpandToSelection();
        QueuePeerScroll();
    }

    private void SelectTableRow(Section section, Table table, TableRow row)
    {
        _target = DesignTarget.TableRow;
        ApplySection(section);
        _control = table;
        _table = table;
        _tableRow = row;
        ExpandToSelection();
        QueuePeerScroll();
    }

    private Grid? AddTargetGrid => _target == DesignTarget.Grid ? _grid : null;

    private void AddSection()
    {
        var section = new Section { Title = "New section", Order = _report.Sections.Count + 1 };
        _report.Sections.Add(section);
        SelectSection(section);
        AfterStructureChanged();
    }

    private void AddSubsection()
    {
        if (_section is null)
            return;

        var child = new Section { Title = "New subsection", Order = _section.Subsections.Count + 1 };
        _section.Subsections.Add(child);
        SelectSection(child);
        AfterStructureChanged();
    }

    private static void AppendControl(Section section, Control control)
    {
        var items = SectionBody.Enumerate(section).ToList();
        items.Add(new() { Kind = SectionBodyKind.Control, Section = section, Control = control });
        SectionBody.WriteOrders(section, items);
    }

    private bool TryPlaceControl(Control control)
    {
        if (_section is null)
            return false;

        if (AddTargetGrid is { } grid) {
            if (!grid.TryAdd(control)) {
                Snackbar.Add("A grid cannot contain another grid.", Severity.Warning);
                return false;
            }

            return true;
        }

        AppendControl(_section, control);
        return true;
    }

    private void AddCard()
    {
        if (_section is null)
            return;

        var card = new Card { Label = "Label", Value = "Value" };
        if (!TryPlaceControl(card))
            return;

        SelectCard(_section, card);
        AfterStructureChanged();
    }

    private void AddTable()
    {
        if (_section is null)
            return;

        var table = new Table { Title = "New table", ShowHeaders = true, Striped = true, Bordered = true };
        table.SetColumnCount(2);
        table.SetRowCount(1);
        if (!TryPlaceControl(table))
            return;

        SelectTable(_section, table);
        AfterStructureChanged();
    }

    private void AddBlock(ContentType type)
    {
        if (_section is null)
            return;

        var block = CreateBlock(type);
        if (!TryPlaceControl(block))
            return;

        SelectBlock(_section, block);
        if (type == ContentType.Component)
            EnsureBindingRows();

        AfterStructureChanged();
    }

    private static Block CreateBlock(ContentType type)
    {
        var block = new Block { ContentType = type };
        switch (type) {
            case ContentType.Text:
                block.Content = "New text";
                break;
            case ContentType.Heading:
                block.Content = "Heading";
                block.Level = 3;
                break;
            case ContentType.Callout:
                block.Content = "Callout";
                block.Tone = "info";
                break;
            case ContentType.Quote:
                block.Content = "Quote";
                break;
            case ContentType.Html:
                block.Content = "<p>New HTML</p>";
                break;
            case ContentType.Code:
                block.Content = "// code";
                break;
            case ContentType.List:
            case ContentType.NumberedList:
                block.ListItems = ["Item 1", "Item 2"];
                break;
            case ContentType.KeyValue:
                block.ListItems = ["Key|Value"];
                break;
            case ContentType.Progress:
                block.Level = 50;
                block.Caption = "Progress";
                break;
            case ContentType.Spacer:
                block.Level = 24;
                break;
            case ContentType.Image:
                block.Source = "https://placehold.co/640x200";
                block.Alt = "Placeholder";
                break;
            case ContentType.Component:
                block.ComponentType = ReportEmbedBinder.KnownComponentTypes.FirstOrDefault();
                break;
            case ContentType.Chart:
                block.ChartKind = ChartKind.Bar;
                block.Caption = "Chart";
                block.Level = 300;
                block.ListItems = ["A|3", "B|5", "C|2"];
                break;
            case ContentType.Badge:
                block.Content = "Status";
                block.Tone = "info";
                break;
            case ContentType.Signature:
                block.Caption = "Authorized by";
                block.Source = "Date";
                break;
            case ContentType.TableOfContents:
                block.Caption = "Contents";
                break;
            case ContentType.Timeline:
                block.ListItems = ["Started|Kickoff", "Done|Complete"];
                break;
            case ContentType.Address:
                block.Caption = "Bill to";
                block.ListItems = ["Acme Corporation", "100 Main Street", "Springfield, ST 00000"];
                break;
            case ContentType.Totals:
                block.ListItems = ["Subtotal|$100.00", "Tax|$8.00", "Total|$108.00"];
                break;
            case ContentType.Checkbox:
                block.Content = "Please confirm:";
                block.ListItems = ["I agree to the terms", "Net 30 payment"];
                break;
            case ContentType.Notes:
                block.Caption = "Notes";
                block.Content = "Delivery window and special instructions.";
                break;
        }

        return block;
    }

    private void AddGrid()
    {
        if (_section is null)
            return;

        var grid = new Grid { Title = "New grid", ColumnCount = 2 };
        AppendControl(_section, grid);
        SelectGrid(_section, grid);
        AfterStructureChanged();
    }

    private void SetTableFieldMap(TableColumn column, string? source)
    {
        if (_table is null)
            return;

        var key = string.IsNullOrWhiteSpace(column.Field) ? column.Header : column.Field;
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (string.IsNullOrWhiteSpace(source))
            _table.FieldMap.Remove(key);
        else
            _table.FieldMap[key] = source;

        AfterStructureChanged();
    }

    private int TableColumnCount => _table?.Columns.Count ?? 1;

    private int TableRowCount => _table?.Rows.Count ?? 0;

    private bool TableRowsEditable => _table is { DataSourceKind: DataSourceKind.Static };

    private void SetTableColumnCount(int count)
    {
        if (_table is null)
            return;

        _table.SetColumnCount(Math.Max(1, count));
        if (_tableColumn is not null && !_table.Columns.Contains(_tableColumn))
            _tableColumn = null;

        AfterStructureChanged();
    }

    private void SetTableRowCount(int count)
    {
        if (_table is null)
            return;

        _table.SetRowCount(Math.Max(0, count));
        if (_tableRow is not null && !_table.Rows.Contains(_tableRow))
            _tableRow = null;

        AfterStructureChanged();
    }

    private void OnTableDataSourceKindChanged(DataSourceKind kind)
    {
        if (_table is null)
            return;

        _table.DataSourceKind = kind;
        SyncTableOptionsKind(kind);
        AfterStructureChanged();
    }

    private static void SyncTableOptionsKind(Table table, DataSourceKind kind)
    {
        if (kind is not DataSourceKind.Query and not DataSourceKind.Sproc)
            return;

        var wanted = kind == DataSourceKind.Query ? ParameterOptionsKind.Query : ParameterOptionsKind.Sproc;
        if (ParameterOptionsJson.TryGetKind(table.Options) != wanted)
            table.Options = ParameterOptionsJson.CreateDefaultForKind(wanted);
    }

    private void SyncTableOptionsKind(DataSourceKind kind)
    {
        if (_table is null)
            return;

        SyncTableOptionsKind(_table, kind);
    }

    private void OnTableOptionsChanged(string? json)
    {
        if (_table is null)
            return;

        _table.Options = json;
        AfterStructureChanged();
    }

    private void DeleteSelected()
    {
        switch (_target) {
            case DesignTarget.Section when _section is not null: {
                var parent = ParentOf(_section);
                if (parent is null)
                    _report.Sections.Remove(_section);
                else
                    parent.Subsections.Remove(_section);

                Reorder(_report.Sections);
                SelectReport();
                break;
            }
            case DesignTarget.Card or DesignTarget.Block or DesignTarget.Table or DesignTarget.Grid when _section is not null && _control is not null: {
                var ownerGrid = FindOwnerGrid(_section, _control);
                if (SectionBody.TryOwner(_section, _control, out var owner) && owner is not null)
                    owner.Remove(_control);

                if (ownerGrid is not null)
                    SelectGrid(_section, ownerGrid);
                else
                    SelectSection(_section);

                break;
            }
            case DesignTarget.TableColumn when _table is not null && _tableColumn is not null: {
                var colIndex = _table.Columns.IndexOf(_tableColumn);
                if (colIndex >= 0) {
                    _table.Columns.RemoveAt(colIndex);
                    foreach (var row in _table.Rows) {
                        if (colIndex < row.Cells.Count)
                            row.Cells.RemoveAt(colIndex);
                    }
                }

                SelectTable(_section!, _table);
                break;
            }
            case DesignTarget.TableRow when _table is not null && _tableRow is not null:
                _table.Rows.Remove(_tableRow);
                SelectTable(_section!, _table);
                break;
        }

        AfterStructureChanged();
    }

    private void MoveSelected(int delta)
    {
        switch (_target) {
            case DesignTarget.Section when _section is not null: {
                var parent = ParentOf(_section);
                if (parent is null) {
                    Move(_report.Sections, _section, delta);
                    Reorder(_report.Sections);
                }
                else {
                    var item = SectionBody.Enumerate(parent).FirstOrDefault(i => i.Kind == SectionBodyKind.Subsection && ReferenceEquals(i.Subsection, _section));
                    if (item is not null)
                        SectionBody.Move(parent, item, delta);
                }

                break;
            }
            case DesignTarget.Card or DesignTarget.Block or DesignTarget.Table or DesignTarget.Grid when _section is not null && _control is not null:
                MoveSelectedControl(_control, delta);
                break;
            case DesignTarget.TableRow when _table is not null && _tableRow is not null:
                Move(_table.Rows, _tableRow, delta);
                break;
            case DesignTarget.TableColumn when _table is not null && _tableColumn is not null: {
                var index = _table.Columns.IndexOf(_tableColumn);
                SectionBody.MoveTableColumn(_table, index, delta);
                break;
            }
        }

        AfterStructureChanged();
    }

    private void MoveSelectedControl(Control control, int delta)
    {
        if (_section is null || !SectionBody.TryOwner(_section, control, out var owner) || owner is null)
            return;

        if (ReferenceEquals(owner, _section.Controls)) {
            var item = SectionBody.Enumerate(_section).FirstOrDefault(i => i.Kind == SectionBodyKind.Control && ReferenceEquals(i.Control, control));
            if (item is not null)
                SectionBody.Move(_section, item, delta);

            return;
        }

        Move(owner, control, delta);
    }

    private static void Move<TItem>(List<TItem> items, TItem item, int delta)
    {
        var index = items.IndexOf(item);
        if (index < 0)
            return;

        var next = index + delta;
        if (next < 0 || next >= items.Count)
            return;

        items.RemoveAt(index);
        items.Insert(next, item);
    }

    private static void Reorder(List<Section> sections)
    {
        for (var i = 0; i < sections.Count; i++)
            sections[i].Order = i + 1;
    }

    private static string CellText(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is JsonElement el)
            return el.ValueKind == JsonValueKind.String ? el.GetString() ?? string.Empty : el.ToString();

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static void SetCell(TableRow row, int index, string? text)
    {
        while (row.Cells.Count <= index)
            row.Cells.Add(null);

        row.Cells[index] = text;
    }

    private static string ListItemsText(Block block) => block.ListItems is null ? string.Empty : string.Join('\n', block.ListItems);

    private static string ListItemsLabel(Block block)
        => block.ContentType switch {
            ContentType.Address => "Address lines (one per line)",
            ContentType.Totals => "Totals (label|amount per line; last row emphasized)",
            ContentType.Checkbox => "Terms (one per line)",
            ContentType.KeyValue => "List items (one per line; key|value for KeyValue)",
            _ => "List items (one per line; key|value for KeyValue)"
        };

    private static void SetListItems(Block block, string? text)
        => block.ListItems = string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimEnd()).ToList();

    private void OnComponentTypeChanged(string? typeName)
    {
        if (_block is null)
            return;

        _block.ComponentType = typeName;
        EnsureBindingRows();
    }

    private void EnsureBindingRows()
    {
        if (_block is null || string.IsNullOrWhiteSpace(_block.ComponentType))
            return;

        var type = ReportEmbedBinder.TryResolveComponent(_block.ComponentType, out _);
        if (type is null)
            return;

        foreach (var prop in ReportEmbedBinder.ParameterProperties(type))
            _block.ParameterBindings.TryAdd(prop.Name, new() { Kind = ComponentBindingKind.Param, Value = prop.Name });
    }

    private IEnumerable<(string Name, ComponentBindingKind Kind, string? Value)> ComponentBindingRows()
    {
        if (_block is null)
            return [];

        EnsureBindingRows();
        return _block.ParameterBindings.Select(kvp => (kvp.Key, kvp.Value.Kind, kvp.Value.Value));
    }

    private void SetBindingKind(string name, ComponentBindingKind kind)
    {
        if (_block is null)
            return;

        if (!_block.ParameterBindings.TryGetValue(name, out var binding))
            _block.ParameterBindings[name] = binding = new();

        binding.Kind = kind;
        AfterStructureChanged();
    }

    private void SetBindingValue(string name, string? value)
    {
        if (_block is null)
            return;

        if (!_block.ParameterBindings.TryGetValue(name, out var binding))
            _block.ParameterBindings[name] = binding = new();

        binding.Value = value;
        AfterStructureChanged();
    }

    private void AddMissingComponentParams()
    {
        if (_block is null)
            return;

        EnsureBindingRows();
        foreach (var kvp in _block.ParameterBindings) {
            if (_paramRows.Any(r => string.Equals(r.Key, kvp.Key, StringComparison.OrdinalIgnoreCase)))
                continue;

            var typeName = LyoTypeInfo.String.FullName;
            var resolved = ReportEmbedBinder.TryResolveComponent(_block.ComponentType, out _);
            var prop = resolved is null ? null : ReportEmbedBinder.ParameterProperties(resolved).FirstOrDefault(p => p.Name == kvp.Key);
            if (prop is not null)
                typeName = prop.PropertyType.FullName ?? typeName;

            _paramRows.Add(new() { Key = kvp.Key, Type = typeName, IsNew = true });
        }

        OnSchemaChanged();
    }

    private static readonly string[] CommonStyleKeys = ["color", "background", "font-size", "font-family", "max-width", "padding", "margin"];

    private void AddStyle(string key)
    {
        var name = key;
        var n = 2;
        while (_report.Styles.ContainsKey(name))
            name = $"{key}-{n++}";

        _report.Styles[name] = "";
        AfterStructureChanged();
    }

    private void RemoveStyle(string key)
    {
        _report.Styles.Remove(key);
        AfterStructureChanged();
    }

    private void RenameStyle(string oldKey, string? newKey)
    {
        if (string.Equals(oldKey, newKey, StringComparison.Ordinal))
            return;

        _report.Styles.TryGetValue(oldKey, out var value);
        _report.Styles.Remove(oldKey);
        if (string.IsNullOrWhiteSpace(newKey))
            return;

        _report.Styles[newKey.Trim()] = value ?? "";
        AfterStructureChanged();
    }

    private void SetStyleValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _report.Styles[key] = ReportHtmlSanitizer.SanitizeCss(value);
        AfterStructureChanged();
    }

    private void Undo()
    {
        if (_undo.Count == 0)
            return;

        _redo.Add(_snapshot);
        RestoreSnapshot(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
    }

    private void Redo()
    {
        if (_redo.Count == 0)
            return;

        _undo.Add(_snapshot);
        RestoreSnapshot(_redo[^1]);
        _redo.RemoveAt(_redo.Count - 1);
    }

    private void RestoreSnapshot(string json)
    {
        _restoring = true;
        try {
            _report = ReportJson.Deserialize<object>(json);
            _report.Layout ??= new();
            LoadRowsFromSpecs();
            SelectReport();
            SeedExpandedDefaults();
            RefreshJsonDraft();
            _snapshot = _jsonDraft;
            _dirty = true;
        }
        finally {
            _restoring = false;
            StateHasChanged();
        }
    }

    private bool CanDuplicate => _target is not DesignTarget.Report;

    private void DuplicateSelected()
    {
        switch (_target) {
            case DesignTarget.Section when _section is not null: {
                var clone = CloneNode(_section);
                var parent = ParentOf(_section);
                var list = parent?.Subsections ?? _report.Sections;
                var index = list.IndexOf(_section);
                list.Insert(index + 1, clone);
                Reorder(list);
                SelectSection(clone);
                break;
            }
            case DesignTarget.Card or DesignTarget.Block or DesignTarget.Table or DesignTarget.Grid when _section is not null && _control is not null:
                DuplicateControl(_control);
                break;
            case DesignTarget.TableColumn when _section is not null && _table is not null && _tableColumn is not null: {
                var clone = CloneNode(_tableColumn);
                var index = _table.Columns.IndexOf(_tableColumn);
                _table.Columns.Insert(index + 1, clone);
                foreach (var row in _table.Rows)
                    row.Cells.Insert(Math.Min(index + 1, row.Cells.Count), index < row.Cells.Count ? row.Cells[index] : null);

                SelectTableColumn(_section, _table, clone);
                break;
            }
            case DesignTarget.TableRow when _section is not null && _table is not null && _tableRow is not null: {
                var clone = CloneNode(_tableRow);
                var index = _table.Rows.IndexOf(_tableRow);
                _table.Rows.Insert(index + 1, clone);
                SelectTableRow(_section, _table, clone);
                break;
            }
            default:
                return;
        }

        AfterStructureChanged();
    }

    private void DuplicateControl(Control control)
    {
        if (_section is null)
            return;

        var clone = CloneNode(control);
        if (!SectionBody.TryOwner(_section, control, out var owner) || owner is null)
            return;

        if (ReferenceEquals(owner, _section.Controls)) {
            var items = SectionBody.Enumerate(_section).ToList();
            var index = items.FindIndex(i => i.Kind == SectionBodyKind.Control && ReferenceEquals(i.Control, control));
            if (index < 0)
                return;

            items.Insert(index + 1, new() { Kind = SectionBodyKind.Control, Section = _section, Control = clone });
            SectionBody.WriteOrders(_section, items);
        }
        else {
            var index = owner.IndexOf(control);
            owner.Insert(index + 1, clone);
        }

        SelectControl(_section, clone);
    }

    private static T CloneNode<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, ReportJson.Options), ReportJson.Options)!;

    private void OnCanvasCardDrop(ReportDesignCardDrop drop)
    {
        foreach (var root in _report.Sections) {
            if (!SectionBody.RelocateToGrid(root, drop.Control, drop.Grid, drop.InsertIndex))
                continue;

            var section = AllSections().FirstOrDefault(s => SectionBody.WalkControls(s.Controls).Contains(drop.Control)) ?? root;
            SelectControl(section, drop.Control);
            AfterStructureChanged();
            return;
        }
    }

    private void OnCanvasGridColumnDrop(ReportDesignGridColumnDrop drop)
    {
        var from = drop.Table.Columns.IndexOf(drop.Column);
        if (from < 0 || !SectionBody.MoveTableColumnTo(drop.Table, from, drop.InsertIndex))
            return;

        if (_section is not null)
            SelectTableColumn(_section, drop.Table, drop.Column);

        AfterStructureChanged();
    }

    private void OnDesignKey(KeyboardEventArgs e)
    {
        if (!(e.CtrlKey || e.MetaKey) || e.AltKey)
            return;

        if (string.Equals(e.Key, "z", StringComparison.OrdinalIgnoreCase) && !e.ShiftKey) {
            Undo();
            return;
        }

        if (string.Equals(e.Key, "y", StringComparison.OrdinalIgnoreCase) || (string.Equals(e.Key, "z", StringComparison.OrdinalIgnoreCase) && e.ShiftKey)) {
            Redo();
            return;
        }

        if (string.Equals(e.Key, "d", StringComparison.OrdinalIgnoreCase) && CanDuplicate)
            DuplicateSelected();
    }

    private bool ShowBlockContent(ContentType type)
        => type is ContentType.Text or ContentType.Heading or ContentType.Callout or ContentType.Quote
            or ContentType.Code or ContentType.Html or ContentType.Badge or ContentType.Checkbox
            or ContentType.Notes or ContentType.Signature;

    private bool ShowBlockCaption(ContentType type)
        => type is ContentType.Image or ContentType.Chart or ContentType.TableOfContents or ContentType.Address
            or ContentType.Notes or ContentType.Signature or ContentType.Progress;

    private bool ShowBlockSource(ContentType type)
        => type is ContentType.Image or ContentType.Signature;

    private bool ShowBlockAlt(ContentType type) => type is ContentType.Image;

    private bool ShowBlockTone(ContentType type) => type is ContentType.Callout or ContentType.Badge;

    private bool ShowBlockLevel(ContentType type)
        => type is ContentType.Heading or ContentType.Progress or ContentType.Spacer;

    private bool ShowBlockListItems(ContentType type)
        => type is ContentType.List or ContentType.NumberedList or ContentType.KeyValue or ContentType.Timeline
            or ContentType.Address or ContentType.Totals or ContentType.Checkbox or ContentType.TableOfContents;

    private string InspectorTitle => _target switch {
        DesignTarget.Section => _section?.Title ?? "Section",
        DesignTarget.Card => _card?.Label ?? "Card",
        DesignTarget.Block => _block?.ContentType.ToString() ?? "Block",
        DesignTarget.Table => _table?.Title ?? "Table",
        DesignTarget.Grid => _grid?.Title ?? "Grid",
        DesignTarget.TableColumn => _tableColumn?.Header ?? "Column",
        DesignTarget.TableRow => "Row",
        _ => "Report"
    };

    private sealed class DraftParam : LyoParameterDefinitionBase;
}
