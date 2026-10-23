using System.Text.Json;
using Lyo.Algorithms.Client;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Lyo.Algorithms.Models.Response;
using Lyo.Http.Client;
using Lyo.Web.Components;
using Lyo.Web.Components.ParamTable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Lyo.Algorithms.Web.Components;

/// <summary>Scrollable catalog plus an editor tab and a runner tab. Preview runs only when the runner asks.</summary>
public partial class AlgorithmWorkbench : IAsyncDisposable
{
    private const int SaveDelayMs = 450;

    private readonly List<AlgorithmDefinitionRes> _definitions = [];
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);
    private readonly List<AlgorithmRevisionRes> _revisions = [];
    private AlgorithmDraft _draft = new();
    private List<AlgorithmContextRow> _context = [];
    private List<LyoParameterEntry> _entries = [];
    private AlgorithmPreviewRes? _preview;
    private string? _entrySignature;
    private string? _error;
    private string? _focused;
    private bool _ready;
    private CancellationTokenSource? _saveCts;

    [Inject]
    private IAlgorithmClient Client { get; set; } = null!;

    [Inject]
    private ClientStore ClientStore { get; set; } = null!;

    private IReadOnlyList<string> BagNames
        => _draft.Parameters.Select(row => row.Name).Concat(_context.Select(row => row.Name)).Where(AlgorithmDraftRules.IsIdentifier).Distinct(StringComparer.Ordinal).ToArray();

    private IReadOnlyList<string> CatalogKeys => _definitions.Select(row => row.Key).ToArray();

    private IReadOnlyList<string> FormulaNames => AlgorithmExpressionHelp.Identifiers(_draft.Source);

    private bool CanSave => AlgorithmKeys.IsKey(_draft.Key) && _draft.Issues().Count == 0;

    protected override async Task OnInitializedAsync()
    {
        await RestoreAsync();
        SyncEntries();
        try {
            await ReloadAsync();
        }
        catch (Exception ex) {
            _error = ResponseMessage(ex);
        }

        _ready = true;
    }

    public async ValueTask DisposeAsync()
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        try {
            await PersistAsync();
        }
        catch (JSDisconnectedException) {
        }
    }

    private async Task ReloadAsync()
    {
        var rows = await Client.ListAsync();
        _definitions.Clear();
        _definitions.AddRange(rows);
        _selected.RemoveWhere(key => _definitions.All(row => row.Key != key));
    }

    private void NewDraft()
    {
        _draft = new();
        _context = [];
        _revisions.Clear();
        _preview = null;
        _entrySignature = null;
        SyncEntries();
        QueuePersist();
    }

    private void LoadFishing() => Apply(AlgorithmPresets.FishingDraft(), AlgorithmPresets.FishingContext());

    private void LoadAttack() => Apply(AlgorithmPresets.AttackDraft(), AlgorithmPresets.AttackContext());

    private void LoadSchedule() => Apply(AlgorithmPresets.ScheduleDraft(), AlgorithmPresets.ScheduleContext());

    private void Apply(AlgorithmDraft draft, List<AlgorithmContextRow> context)
    {
        _draft = draft;
        _context = context;
        DeclareContextParameters();
        _preview = null;
        _entrySignature = null;
        SyncEntries();
        QueuePersist();
    }

    private async Task OpenKey(string key)
    {
        var revisions = await Client.GetRevisionsAsync(key);
        _revisions.Clear();
        _revisions.AddRange(revisions);
        if (revisions.Count > 0)
            OpenRevision(revisions[0]);

        _draft.Key = key;
        var definition = _definitions.FirstOrDefault(row => row.Key == key);
        _draft.Name = definition?.Name;
        _draft.Description = definition?.Description;
        QueuePersist();
    }

    private void OpenRevision(AlgorithmRevisionRes revision)
    {
        var key = _draft.Key;
        var name = _draft.Name;
        _draft = AlgorithmDraft.FromRevision(revision);
        _draft.Key = key;
        _draft.Name = name;
        _preview = null;
        _entrySignature = null;
        SyncEntries();
        QueuePersist();
    }

    private Task SetKind(AlgorithmKind kind)
    {
        _draft.Kind = kind;
        return OnChanged();
    }

    private Task SetSource(string source)
    {
        _draft.Source = source;
        return OnChanged();
    }

    private Task SetFocused(string? name)
    {
        _focused = name;
        return Task.CompletedTask;
    }

    private Task OnChanged()
    {
        SyncEntries();
        QueuePersist();
        return Task.CompletedTask;
    }

    private Task OnValuesChanged()
    {
        RememberEntries();
        QueuePersist();
        return Task.CompletedTask;
    }

    private void OnChip(string name)
    {
        _focused = name;
        if (_draft.Parameters.Any(row => string.Equals(row.Name, name, StringComparison.Ordinal)))
            return;

        AddMissing(name);
    }

    private void AddMissing(string name)
    {
        var alias = _draft.Parameters.FirstOrDefault(row => string.Equals(row.Name, name, StringComparison.Ordinal))?.Alias ?? AlgorithmDraftRules.DefaultAlias(name);
        _draft.SetParameter(name, alias);
        if (_context.All(row => !string.Equals(row.Name, name, StringComparison.Ordinal)))
            _context.Add(new() { Name = name, Alias = alias });

        _entrySignature = null;
        SyncEntries();
        QueuePersist();
    }

    private void DeclareContextParameters()
    {
        if (_draft.Parameters.Any(row => AlgorithmDraftRules.IsIdentifier(row.Name)))
            return;

        foreach (var row in _context.Where(row => AlgorithmDraftRules.IsIdentifier(row.Name)))
            _draft.SetParameter(row.Name, row.Alias);
    }

    private void SyncEntries()
    {
        foreach (var parameter in _draft.Parameters.Where(row => AlgorithmDraftRules.IsIdentifier(row.Name))) {
            var existing = _context.FirstOrDefault(row => string.Equals(row.Name, parameter.Name, StringComparison.Ordinal));
            if (existing is null)
                _context.Add(new() { Name = parameter.Name, Alias = parameter.Alias });
            else
                existing.Alias = parameter.Alias;
        }

        var signature = string.Join("|", _draft.Parameters.Where(row => AlgorithmDraftRules.IsIdentifier(row.Name)).Select(row => row.Name + "\n" + row.Alias));
        if (signature == _entrySignature)
            return;

        _entrySignature = signature;
        _entries = _draft.Parameters.Where(row => AlgorithmDraftRules.IsIdentifier(row.Name)).Select(parameter => {
            var row = _context.First(item => string.Equals(item.Name, parameter.Name, StringComparison.Ordinal));
            var json = AlgorithmFieldJson.ToField(parameter.Alias, row.Text);
            return new LyoParameterEntry(new AlgorithmParameterDefinition(parameter.Name, AlgorithmFieldJson.ClrTypeName(parameter.Alias), json));
        }).ToList();
    }

    private void RememberEntries()
    {
        foreach (var entry in _entries) {
            var parameter = _draft.Parameters.FirstOrDefault(row => string.Equals(row.Name, entry.Definition.Key, StringComparison.Ordinal));
            if (parameter is null)
                continue;

            var text = AlgorithmFieldJson.FromField(parameter.Alias, entry.Value);
            var row = _context.FirstOrDefault(item => string.Equals(item.Name, parameter.Name, StringComparison.Ordinal));
            if (row is null)
                _context.Add(new() { Name = parameter.Name, Alias = parameter.Alias, Text = text });
            else {
                row.Alias = parameter.Alias;
                row.Text = text;
            }
        }
    }

    private IEnumerable<AlgorithmContextRow> DeclaredRows()
        => _draft.Parameters.Where(row => AlgorithmDraftRules.IsIdentifier(row.Name)).Select(parameter =>
            _context.FirstOrDefault(row => string.Equals(row.Name, parameter.Name, StringComparison.Ordinal)) ?? new AlgorithmContextRow { Name = parameter.Name, Alias = parameter.Alias });

    private async Task SaveAsync()
    {
        _error = null;
        try {
            await Client.SaveRevisionAsync(_draft.Key, _draft.ToRevision());
            await ReloadAsync();
            await OpenKey(_draft.Key);
        }
        catch (Exception ex) {
            _error = ResponseMessage(ex);
        }
    }

    private async Task RevertAsync(int revision)
    {
        _error = null;
        try {
            await Client.RevertAsync(_draft.Key, new() { Revision = revision });
            await OpenKey(_draft.Key);
        }
        catch (Exception ex) {
            _error = ResponseMessage(ex);
        }
    }

    private async Task SetLabelAsync((string Label, AlgorithmLabelReq Request) label)
    {
        _error = null;
        try {
            await Client.SetLabelAsync(_draft.Key, label.Label, label.Request);
        }
        catch (Exception ex) {
            _error = ResponseMessage(ex);
        }
    }

    private async Task RunAsync()
    {
        RememberEntries();
        _preview = null;
        _error = null;
        try {
            var context = JsonSerializer.Deserialize<JsonElement>(AlgorithmContextRows.ToJson(DeclaredRows()));
            _preview = await Client.PreviewAsync(
                new() { Key = _draft.Key, Kind = _draft.Kind, Source = _draft.Source, Payload = _draft.ToPayload(), Context = context });
        }
        catch (Exception ex) {
            _preview = null;
            _error = ResponseMessage(ex);
        }
    }

    private async Task DeleteCurrentAsync() => await DeleteKeysAsync([_draft.Key]);

    private async Task DeleteSelectedAsync() => await DeleteKeysAsync(_selected.ToArray());

    private async Task DeleteKeysAsync(IReadOnlyList<string> keys)
    {
        var failures = new List<string>();
        var removed = new List<string>();
        foreach (var key in keys) {
            try {
                await Client.DeleteAsync(key);
                _selected.Remove(key);
                removed.Add(key);
            }
            catch (Exception ex) {
                failures.Add(ResponseMessage(ex));
            }
        }

        await ReloadAsync();
        if (removed.Contains(_draft.Key))
            NewDraft();

        _error = failures.Count == 0 ? null : string.Join(" ", failures);
    }

    private void ToggleSelected(string key, bool selected)
    {
        if (selected)
            _selected.Add(key);
        else
            _selected.Remove(key);
    }

    private void QueuePersist()
    {
        if (!_ready)
            return;

        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts = new();
        _ = PersistLaterAsync(_saveCts.Token);
    }

    private async Task PersistLaterAsync(CancellationToken token)
    {
        try {
            await Task.Delay(SaveDelayMs, token);
            await PersistAsync();
        }
        catch (OperationCanceledException) {
        }
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(new Persisted(_draft, _context));
        await ClientStore.SetAlgorithmWorkbenchStateAsync(json);
    }

    private async Task RestoreAsync()
    {
        var json = await ClientStore.GetAlgorithmWorkbenchStateAsync();
        if (string.IsNullOrWhiteSpace(json))
            return;

        var state = JsonSerializer.Deserialize<Persisted>(json);
        if (state?.Draft is null)
            return;

        _draft = state.Draft;
        _context = state.Context ?? [];
    }

    private static string ResponseMessage(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException) {
            if (current is LyoHttpException http && !string.IsNullOrWhiteSpace(http.ResponseBody))
                return BodyMessage(http.ResponseBody);
        }

        return ex.Message;
    }

    private static string BodyMessage(string body)
    {
        try {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
                return root.GetString() ?? body;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(detail.GetString()))
                return detail.GetString()!;

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(message.GetString()))
                return message.GetString()!;
        }
        catch (JsonException) {
            return body;
        }

        return body;
    }

    private static string Format(object? value) => value is null ? string.Empty : JsonSerializer.Serialize(value);

    private sealed record Persisted(AlgorithmDraft Draft, List<AlgorithmContextRow> Context);
}
