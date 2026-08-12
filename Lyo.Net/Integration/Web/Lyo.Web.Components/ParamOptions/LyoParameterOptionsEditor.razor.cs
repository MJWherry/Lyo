using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Parameters;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamOptions;

/// <summary>Edits definition parameter <c>Options</c> JSON — static key/label items or a root <see cref="QueryReq" /> via <c>QueryRootForm</c>.</summary>
public partial class LyoParameterOptionsEditor : ComponentBase
{
    private readonly List<ParameterOptionsItem> _items = [];
    private string? _error;
    private ParameterOptionsKind? _kind;
    private string? _lastIncoming;
    private QueryReq _query = CreateDefaultQuery();
    private string? _queryRoute;

    /// <summary>Serialized Options JSON bound to the definition parameter.</summary>
    [Parameter]
    public string? OptionsJson { get; set; }

    [Parameter]
    public EventCallback<string?> OptionsJsonChanged { get; set; }

    /// <summary>
    /// When true (default), renders the Options kind MudSelect. When false, the parent owns the kind control (e.g. definition parameter table column) and this editor only shows
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
}