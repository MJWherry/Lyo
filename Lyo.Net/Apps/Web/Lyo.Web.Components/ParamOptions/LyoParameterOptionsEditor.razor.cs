using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamOptions;

/// <summary>Edits definition parameter <c>Options</c> JSON. static key/label items or a root <see cref="QueryReq" /> via <c>QueryRootForm</c>.</summary>
public partial class LyoParameterOptionsEditor : ComponentBase
{
    private readonly List<ParameterOptionsItem> _items = [];
    private string? _error;
    private ParameterOptionsKind? _kind;
    private string? _lastIncoming;
    private QueryReq _query = CreateDefaultQuery();
    private string? _queryRoute;
    private string? _storedProcName;
    private string? _keyField;
    private string? _labelField;
    private readonly List<(string Name, string Value)> _sprocArgs = [];

    /// <summary>Serialized Options JSON bound to the definition parameter.</summary>
    [Parameter]
    public string? OptionsJson { get; set; }

    [Parameter]
    public EventCallback<string?> OptionsJsonChanged { get; set; }

    /// <summary>
    /// When true (default), renders the Options kind MudSelect. If false, the parent owns the kind control (for example definition parameter table column) and this editor
    /// only shows
    /// kind-specific fields.
    /// </summary>
    [Parameter]
    public bool ShowKindSelect { get; set; } = true;

    protected override void OnParametersSet()
    {
        if (string.Equals(OptionsJson, _lastIncoming, StringComparison.Ordinal))
            return;

        _lastIncoming = OptionsJson;
        _error = null;
        _items.Clear();
        _kind = null;
        _queryRoute = null;
        _query = CreateDefaultQuery();
        _storedProcName = null;
        _keyField = null;
        _labelField = null;
        _sprocArgs.Clear();
        if (string.IsNullOrWhiteSpace(OptionsJson))
            return;

        if (!ParameterOptionsJson.TryDeserialize(OptionsJson, out var options) || options is null) {
            _error = "Invalid Options JSON.";
            return;
        }

        _kind = options.Kind;
        if (options.Kind == ParameterOptionsKind.Static) {
            _items.AddRange(options.Items.Select(i => new ParameterOptionsItem(i.Key, i.Label)));
            if (_items.Count == 0)
                _items.Add(new());
        }
        else if (options.Kind == ParameterOptionsKind.Query) {
            _queryRoute = options.QueryRoute;
            _query = NormalizeQuery(options.Query) ?? CreateDefaultQuery();
        }
        else if (options.Kind == ParameterOptionsKind.Sproc) {
            _storedProcName = options.StoredProcName;
            _keyField = options.KeyField;
            _labelField = options.LabelField;
            foreach (var kvp in options.SprocParameters)
                _sprocArgs.Add((kvp.Key, kvp.Value));
        }
    }

    private async Task OnKindChanged(ParameterOptionsKind? kind)
    {
        _kind = kind;
        _error = null;
        if (kind is null) {
            await EmitAsync(null);
            return;
        }

        if (kind == ParameterOptionsKind.Static && _items.Count == 0)
            _items.Add(new());

        if (kind == ParameterOptionsKind.Query)
            _query = NormalizeQuery(_query) ?? CreateDefaultQuery();

        if (kind == ParameterOptionsKind.Sproc && _sprocArgs.Count == 0)
            _sprocArgs.Add(("", "{{Key}}"));

        await EmitCurrentAsync();
    }

    private Task AddItem()
    {
        _items.Add(new());
        return EmitCurrentAsync();
    }

    private Task RemoveItem(int index)
    {
        if (index < 0 || index >= _items.Count)
            return Task.CompletedTask;

        _items.RemoveAt(index);
        return EmitCurrentAsync();
    }

    private Task SetItem(int index, string? key, string? label)
    {
        if (index < 0 || index >= _items.Count)
            return Task.CompletedTask;

        _items[index].Key = key ?? "";
        _items[index].Label = label ?? "";
        return EmitCurrentAsync();
    }

    private Task OnQueryRouteChanged(string? route)
    {
        _queryRoute = route;
        return EmitCurrentAsync();
    }

    private Task OnQueryChanged(QueryReq query)
    {
        _query = NormalizeQuery(query) ?? CreateDefaultQuery();
        return EmitCurrentAsync();
    }

    private async Task EmitCurrentAsync()
    {
        _error = null;
        if (_kind is null) {
            await EmitAsync(null);
            return;
        }

        try {
            ParameterOptions options;
            if (_kind == ParameterOptionsKind.Static) {
                options = new() {
                    Kind = ParameterOptionsKind.Static,
                    Items = _items.Where(i => !string.IsNullOrWhiteSpace(i.Key))
                        .Select(i => new ParameterOptionsItem(i.Key.Trim(), string.IsNullOrWhiteSpace(i.Label) ? i.Key.Trim() : i.Label.Trim()))
                        .ToList()
                };

                if (options.Items.Count == 0) {
                    await EmitAsync(null);
                    return;
                }
            }
            else if (_kind == ParameterOptionsKind.Sproc) {
                options = new() {
                    Kind = ParameterOptionsKind.Sproc,
                    StoredProcName = string.IsNullOrWhiteSpace(_storedProcName) ? null : _storedProcName.Trim(),
                    KeyField = string.IsNullOrWhiteSpace(_keyField) ? null : _keyField.Trim(),
                    LabelField = string.IsNullOrWhiteSpace(_labelField) ? null : _labelField.Trim(),
                    SprocParameters = _sprocArgs
                        .Where(a => !string.IsNullOrWhiteSpace(a.Name))
                        .ToDictionary(a => a.Name.Trim(), a => a.Value, StringComparer.OrdinalIgnoreCase)
                };

                if (!ParameterOptionsJson.IsValidStoredProcName(options.StoredProcName))
                    _error = "Stored proc name must be schema.func (letters, digits, underscore).";
            }
            else {
                var query = NormalizeQuery(_query) ?? CreateDefaultQuery();
                if (string.IsNullOrWhiteSpace(query.From.EntityType))
                    _error = "Set From.EntityType (table) so the Value picker can load options.";
                else if (query.Select.Count == 0)
                    _error = "Add at least one Select path (e.g. c.Id, c.Name).";

                options = new() { Kind = ParameterOptionsKind.Query, QueryRoute = string.IsNullOrWhiteSpace(_queryRoute) ? null : _queryRoute.Trim(), Query = query };
            }

            await EmitAsync(ParameterOptionsJson.Serialize(options));
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
    }

    private async Task EmitAsync(string? json)
    {
        _lastIncoming = json;
        OptionsJson = json;
        if (OptionsJsonChanged.HasDelegate)
            await OptionsJsonChanged.InvokeAsync(json);
    }

    private static QueryReq CreateDefaultQuery()
        => new() {
            From = new() { Alias = "c", EntityType = "" },
            Select = ["c.Id", "c.Name"],
            ComputedFields = [new("Key", "{c.Id}"), new("Value", "{c.Name}")],
            Amount = 200,
            Options = new() { TotalCountMode = QueryTotalCountMode.None }
        };

    private static QueryReq? NormalizeQuery(QueryReq? query)
    {
        if (query is null)
            return null;

        query.From ??= new();
        query.Joins ??= [];
        query.Select ??= [];
        query.ComputedFields ??= [];
        query.SortBy ??= [];
        query.Keys ??= [];
        query.Include ??= [];
        query.Options ??= new();
        return query;
    }

    private Task OnStoredProcNameChanged(string? name)
    {
        _storedProcName = name;
        return EmitCurrentAsync();
    }

    private Task OnKeyFieldChanged(string? value)
    {
        _keyField = value;
        return EmitCurrentAsync();
    }

    private Task OnLabelFieldChanged(string? value)
    {
        _labelField = value;
        return EmitCurrentAsync();
    }

    private Task AddSprocArg()
    {
        _sprocArgs.Add(("", ""));
        return EmitCurrentAsync();
    }

    private Task RemoveSprocArg(int index)
    {
        if (index < 0 || index >= _sprocArgs.Count)
            return Task.CompletedTask;

        _sprocArgs.RemoveAt(index);
        return EmitCurrentAsync();
    }

    private Task SetSprocArg(int index, string? name, string? value)
    {
        if (index < 0 || index >= _sprocArgs.Count)
            return Task.CompletedTask;

        _sprocArgs[index] = (name ?? "", value ?? "");
        return EmitCurrentAsync();
    }
}