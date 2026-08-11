using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Parameters;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Lyo.Web.Components.ParamOptions;

/// <summary>
/// MudSelect fed by definition parameter <c>Options</c> JSON (static items or root <c>/Query</c>) or JSON-array
/// <c>AllowedValues</c>. Selected key(s) bind to <see cref="Value" /> (multi = JSON array via <see cref="ParameterListJson" />).
/// </summary>
public partial class LyoParameterOptionsSelect : ComponentBase, IAsyncDisposable
{
    private readonly List<ParameterOptionsItem> _items = [];
    private string? _helper;
    private bool _loading;
    private string? _lastLoadFingerprint;
    private CancellationTokenSource? _loadCts;

    /// <summary>API client used for root query option loads.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Definition parameter Options JSON (static or query). Preferred over <see cref="AllowedValues" /> when set.</summary>
    [Parameter]
    public string? OptionsJson { get; set; }

    /// <summary>JSON-array fallback when <see cref="OptionsJson" /> is empty.</summary>
    [Parameter]
    public string? AllowedValues { get; set; }

    /// <summary>Sibling parameter key → current value for <c>{{Key}}</c> binding in query options.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string?>? SiblingValues { get; set; }

    /// <summary>Selected option key, or JSON-array keys when <see cref="AllowMultiple" />.</summary>
    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool AllowMultiple { get; set; }

    [Parameter]
    public bool Required { get; set; }

    /// <summary>Wire shape for multi <see cref="Value" /> JSON array elements (defaults to strings for option keys).</summary>
    [Parameter]
    public ParameterListJsonKind ListKind { get; set; } = ParameterListJsonKind.String;

    [Inject]
    private ILogger<LyoParameterOptionsSelect>? Logger { get; set; }

    private IReadOnlyCollection<string> SelectedValues
        => ParameterListJson.Parse(Value);

    protected override async Task OnParametersSetAsync()
    {
        var fingerprint = BuildFingerprint();
        if (fingerprint == _lastLoadFingerprint)
            return;

        _lastLoadFingerprint = fingerprint;
        await ReloadAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_loadCts is not null) {
            await _loadCts.CancelAsync();
            _loadCts.Dispose();
            _loadCts = null;
        }
    }

    private async Task OnValueChanged(string? value)
    {
        if (AllowMultiple)
            return;

        Value = value;
        if (ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync(value);
    }

    private async Task OnSelectedValuesChanged(IReadOnlyCollection<string>? values)
    {
        Value = ParameterListJson.Serialize(values, ListKind);
        if (ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync(Value);
    }

    private static string MultiText(IReadOnlyList<string?>? selected)
        => string.Join(", ", (selected ?? []).Where(s => !string.IsNullOrWhiteSpace(s)));

    private string BuildFingerprint()
    {
        var sib = SiblingValues is null
            ? ""
            : string.Join(";", SiblingValues.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).Select(kv => $"{kv.Key}={kv.Value}"));
        return $"{OptionsJson}\n{AllowedValues}\n{ListKind}\n{sib}";
    }

    private async Task ReloadAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new();
        var ct = _loadCts.Token;

        _items.Clear();
        _helper = null;
        _loading = true;
        await InvokeAsync(StateHasChanged);

        try {
            if (ParameterOptionsJson.TryDeserialize(OptionsJson, out var options) && options is not null) {
                switch (options.Kind) {
                    case ParameterOptionsKind.Static:
                        _items.AddRange(options.Items.Where(i => !string.IsNullOrEmpty(i.Key)));
                        break;
                    case ParameterOptionsKind.Query:
                        await LoadQueryOptionsAsync(options, ct);
                        break;
                    default:
                        _helper = $"Unsupported options kind '{options.Kind}'.";
                        break;
                }
            }
            else if (!string.IsNullOrWhiteSpace(OptionsJson)) {
                _helper = "Invalid Options JSON.";
            }
            else {
                _items.AddRange(ParameterOptionsBinder.FromAllowedValues(AllowedValues));
            }

            PruneStaleSelection();
        }
        catch (OperationCanceledException) {
            // superseded load
        }
        catch (ApiException ex) {
            Logger?.LogWarning(ex, "Failed to load parameter options ({Status})", ex.StatusCode);
            _helper = string.IsNullOrWhiteSpace(ex.Detail)
                ? $"Options query failed ({ex.StatusCode})."
                : ex.Detail;
            _items.Clear();
        }
        catch (Exception ex) {
            Logger?.LogWarning(ex, "Failed to load parameter options");
            _helper = string.IsNullOrWhiteSpace(ex.Message) ? "Failed to load options." : ex.Message;
            _items.Clear();
        }
        finally {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadQueryOptionsAsync(ParameterOptions options, CancellationToken ct)
    {
        if (options.Query is null) {
            _helper = "Query options are missing a QueryReq template.";
            return;
        }

        var siblings = SiblingValues ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!ParameterOptionsBinder.TryBind(options.Query, siblings, out var bound, out var missing) || bound is null) {
            _helper = missing.Count > 0 ? $"Set {string.Join(", ", missing)} first." : "Unable to bind query options.";
            return;
        }

        var route = options.EffectiveQueryRoute;
        var res = await ApiClient.PostAsAsync<QueryReq, ProjectedQueryRes<object?>>(route, bound, ct: ct);
        if (res is null || !res.IsSuccess) {
            _helper = res?.Error?.GetFullMessage() ?? "Options query failed.";
            return;
        }

        foreach (var row in res.Items ?? []) {
            if (ParameterOptionsBinder.TryReadKeyValue(row, bound.Select, out var key, out var label))
                _items.Add(new ParameterOptionsItem(key, string.IsNullOrEmpty(label) ? key : label));
        }

        if (_items.Count == 0)
            _helper = "No options returned.";
    }

    private void PruneStaleSelection()
    {
        if (string.IsNullOrEmpty(Value) || _items.Count == 0)
            return;

        var keys = new HashSet<string>(_items.Select(i => i.Key), StringComparer.OrdinalIgnoreCase);
        if (AllowMultiple) {
            var kept = SelectedValues.Where(keys.Contains).ToList();
            var joined = ParameterListJson.Serialize(kept, ListKind);
            if (!string.Equals(joined, Value, StringComparison.Ordinal))
                _ = ValueChanged.InvokeAsync(joined);
            Value = joined;
        }
        else if (!keys.Contains(Value)) {
            Value = null;
            _ = ValueChanged.InvokeAsync(null);
        }
    }
}
