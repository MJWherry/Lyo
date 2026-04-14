using System.Text.Json;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Lyo.Query.Web.Components;

public partial class QueryBuilderWorkbench : IAsyncDisposable
{
    private const int SaveDebounceMs = 450;

    private CancellationTokenSource? _saveDebounceCts;

    private QueryReq _entityQuery = new() { Start = 0, Amount = 20 };
    private ProjectionQueryReq _projectionQuery = new() { Start = 0, Amount = 20 };
    private List<string> _includeAll = [];
    private List<string> _selectAll = [];
    private List<string> _keysAll = [];
    private QueryWorkbenchRunConfiguration _runConfig = new();
    private QueryWorkbenchRunMode? _trackedRunMode;
    private int _runRestoreKey;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions =
    [
        new("Name", "Name"), new("Id", "Id", FilterPropertyType.Number), new("CreatedAt", "Created At", FilterPropertyType.DateTime), new("IsActive", "Active", FilterPropertyType.Bool), new("Type", "Type")
    ];

    [Inject]
    private ClientStore ClientStore { get; set; } = null!;

    [Inject]
    private JsonSerializerOptions JsonOptions { get; set; } = null!;

    [Inject]
    private ILogger<QueryBuilderWorkbench> Logger { get; set; } = null!;

    [Parameter]
    public string Title { get; set; } = "Query Builder & JSON Editor";

    /// <summary>API base URLs and route segments (per host) when no persisted workbench state exists.</summary>
    [Parameter]
    public Dictionary<string, List<string>>? Routes { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var loadedAny = false;
        try {
            var json = await ClientStore.GetQueryWorkbenchStateAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json)) {
                var loaded = JsonSerializer.Deserialize<QueryWorkbenchPersistedState>(json, JsonOptions);
                if (loaded != null) {
                    loadedAny = true;
                    _projectionQuery = loaded.QueryRequest ?? new ProjectionQueryReq { Start = 0, Amount = 20 };
                    _projectionQuery.Options ??= new ProjectedQueryRequestOptions();
                    _entityQuery = loaded.EntityQuery ?? FromProjectionSharedFields(_projectionQuery);
                    _entityQuery.Options ??= new QueryRequestOptions();
                    _includeAll = loaded.IncludeAll ?? [];
                    _selectAll = loaded.SelectAll ?? [];
                    _keysAll = loaded.KeysAll ?? [];
                    _runConfig = QueryWorkbenchHostNormalization.NormalizeRun(loaded.Run ?? new QueryWorkbenchRunConfiguration());
                    _trackedRunMode = _runConfig.RunMode;
                    _runRestoreKey++;
                }
            }
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Query workbench: could not load persisted state.");
        }

        if (!loadedAny) {
            var hostEndpoints = Routes != null
                ? QueryWorkbenchRunConfiguration.CloneHostEndpoints(Routes)
                : new Dictionary<string, List<string>>();
            _runConfig = QueryWorkbenchHostNormalization.NormalizeRun(new QueryWorkbenchRunConfiguration { HostEndpoints = hostEndpoints });
        }

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts?.Dispose();
        _saveDebounceCts = null;

        try {
            await PersistNowAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Query workbench: final save skipped.");
        }
    }

    private void SchedulePersist()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts?.Dispose();
        _saveDebounceCts = new CancellationTokenSource();
        var ct = _saveDebounceCts.Token;
        _ = DebounceSaveAsync(ct);
    }

    private async Task DebounceSaveAsync(CancellationToken ct)
    {
        try {
            await Task.Delay(SaveDebounceMs, ct).ConfigureAwait(false);
            await PersistNowAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Superseded or disposing.
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Query workbench: could not save state.");
        }
    }

    private async Task PersistNowAsync()
    {
        var state = new QueryWorkbenchPersistedState {
            EntityQuery = _entityQuery,
            QueryRequest = _projectionQuery,
            IncludeAll = _includeAll,
            SelectAll = _selectAll,
            KeysAll = _keysAll,
            Run = _runConfig
        };
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await ClientStore.SetQueryWorkbenchStateAsync(json).ConfigureAwait(false);
    }

    private void OnEntityFormRequestChanged(QueryReq request)
    {
        _entityQuery = request;
        _includeAll = request.Include?.ToList() ?? [];
        SchedulePersist();
    }

    private void OnProjectionFormRequestChanged(ProjectionQueryReq request)
    {
        _projectionQuery = request;
        _selectAll = request.Select?.ToList() ?? [];
        SchedulePersist();
    }

    private void OnRunPanelEntityRequestChanged(QueryReq request)
    {
        _entityQuery = request;
        _includeAll = request.Include?.ToList() ?? [];
        _keysAll = request.Keys?.Select(FormatKeySet).ToList() ?? [];
        SchedulePersist();
    }

    private void OnRunPanelProjectionRequestChanged(ProjectionQueryReq request)
    {
        _projectionQuery = request;
        _selectAll = request.Select?.ToList() ?? [];
        _keysAll = request.Keys?.Select(FormatKeySet).ToList() ?? [];
        SchedulePersist();
    }

    private void OnIncludeAllChanged(IEnumerable<string> includeAll)
    {
        _includeAll = includeAll.ToList();
        SchedulePersist();
    }

    private void OnSelectAllChanged(IEnumerable<string> selectAll)
    {
        _selectAll = selectAll.ToList();
        SchedulePersist();
    }

    private void OnKeysAllChanged(IEnumerable<string> keysAll)
    {
        _keysAll = keysAll.ToList();
        SchedulePersist();
    }

    private Task OnRunConfigurationChanged(QueryWorkbenchRunConfiguration run)
    {
        if (_trackedRunMode.HasValue && _trackedRunMode.Value != run.RunMode) {
            if (run.RunMode == QueryWorkbenchRunMode.QueryProject)
                CopySharedFields(_entityQuery, _projectionQuery);
            else
                CopySharedFields(_projectionQuery, _entityQuery);
        }

        _trackedRunMode = run.RunMode;
        _runConfig = run;
        SchedulePersist();
        return Task.CompletedTask;
    }

    private Task OnWorkbenchEndpointToggleChanged(bool useProject)
    {
        var mode = useProject ? QueryWorkbenchRunMode.QueryProject : QueryWorkbenchRunMode.Query;
        if (_runConfig.RunMode == mode)
            return Task.CompletedTask;

        return OnRunConfigurationChanged(_runConfig with { RunMode = mode });
    }

    private static QueryReq FromProjectionSharedFields(ProjectionQueryReq projection)
    {
        var entity = new QueryReq { Start = projection.Start, Amount = projection.Amount };
        CopySharedFields(projection, entity);
        return entity;
    }

    private static void CopySharedFields(ProjectionQueryReq source, QueryReq target)
    {
        target.Start = source.Start;
        target.Amount = source.Amount;
        target.Keys = CloneKeyRows(source.Keys);
        target.WhereClause = source.WhereClause;
        target.Include = source.Include?.ToList() ?? [];
        target.SortBy = source.SortBy?.ToList() ?? [];
        target.Options ??= new QueryRequestOptions();
        target.Options.TotalCountMode = source.Options.TotalCountMode;
        target.Options.IncludeFilterMode = source.Options.IncludeFilterMode;
    }

    private static void CopySharedFields(QueryReq source, ProjectionQueryReq target)
    {
        target.Start = source.Start;
        target.Amount = source.Amount;
        target.Keys = CloneKeyRows(source.Keys);
        target.WhereClause = source.WhereClause;
        target.Include = source.Include?.ToList() ?? [];
        target.SortBy = source.SortBy?.ToList() ?? [];
        target.Options ??= new ProjectedQueryRequestOptions();
        target.Options.TotalCountMode = source.Options.TotalCountMode;
        target.Options.IncludeFilterMode = source.Options.IncludeFilterMode;
    }

    private static List<object[]> CloneKeyRows(List<object[]>? keys)
    {
        if (keys is null || keys.Count == 0)
            return [];

        return keys.Select(static k => (object[])k.Clone()).ToList();
    }

    private static string FormatKeyPart(object? v)
    {
        if (v == null)
            return "null";

        if (v is string s)
            return $"\"{s}\"";

        if (v is JsonElement je && je.ValueKind == JsonValueKind.String)
            return $"\"{je.GetString() ?? ""}\"";

        return v.ToString() ?? "null";
    }

    private static string FormatKeySet(object[] keySet) => string.Join(", ", keySet.Select(FormatKeyPart));
}
