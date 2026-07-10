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

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("Name", "Name"), new("Id", "Id", FilterPropertyType.Number), new("CreatedAt", "Created At", FilterPropertyType.DateTime),
        new("IsActive", "Active", FilterPropertyType.Bool), new("Type", "Type")
    ];

    private QueryConcreteReq _entityQuery = new() { Start = 0, Amount = 20 };
    private List<string> _includeAll = [];
    private List<string> _keysAll = [];
    private ProjectionQueryReq _projectionQuery = new() { Start = 0, Amount = 20 };
    private QueryReq _rootQuery = CreateDefaultRootQuery();
    private QueryWorkbenchRunConfiguration _runConfig = new();
    private int _runRestoreKey;

    private CancellationTokenSource? _saveDebounceCts;
    private List<string> _selectAll = [];
    private QueryWorkbenchRunMode? _trackedRunMode;

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

    protected override async Task OnInitializedAsync()
    {
        var loadedAny = false;
        try {
            var json = await ClientStore.GetQueryWorkbenchStateAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json)) {
                var loaded = JsonSerializer.Deserialize<QueryWorkbenchPersistedState>(json, JsonOptions);
                if (loaded != null) {
                    loadedAny = true;
                    _projectionQuery = loaded.QueryRequest;
                    _entityQuery = loaded.EntityQuery ?? FromProjectionSharedFields(_projectionQuery);
                    _rootQuery = loaded.RootQuery ?? CreateDefaultRootQuery();
                    EnsureRootQueryShape(_rootQuery);
                    _includeAll = loaded.IncludeAll;
                    _selectAll = loaded.SelectAll;
                    _keysAll = loaded.KeysAll;
                    _runConfig = QueryWorkbenchHostNormalization.NormalizeRun(loaded.Run);
                    _trackedRunMode = _runConfig.RunMode;
                    _runRestoreKey++;
                }
            }
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Query workbench: could not load persisted state.");
        }

        if (!loadedAny) {
            var hostEndpoints = Routes != null ? QueryWorkbenchRunConfiguration.CloneHostEndpoints(Routes) : new();
            _runConfig = QueryWorkbenchHostNormalization.NormalizeRun(new() { HostEndpoints = hostEndpoints });
        }

        await InvokeAsync(StateHasChanged);
    }

    private void SchedulePersist()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts?.Dispose();
        _saveDebounceCts = new();
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
            RootQuery = _rootQuery,
            IncludeAll = _includeAll,
            SelectAll = _selectAll,
            KeysAll = _keysAll,
            Run = _runConfig
        };

        var json = JsonSerializer.Serialize(state, JsonOptions);
        await ClientStore.SetQueryWorkbenchStateAsync(json).ConfigureAwait(false);
    }

    private void OnEntityFormRequestChanged(QueryConcreteReq request)
    {
        _entityQuery = request;
        _includeAll = request.Include.ToList();
        SchedulePersist();
    }

    private void OnProjectionFormRequestChanged(ProjectionQueryReq request)
    {
        _projectionQuery = request;
        _selectAll = request.Select.ToList();
        SchedulePersist();
    }

    private void OnRootFormRequestChanged(QueryReq request)
    {
        _rootQuery = request;
        EnsureRootQueryShape(_rootQuery);
        _selectAll = request.Select.ToList();
        SchedulePersist();
    }

    private void OnRunPanelEntityRequestChanged(QueryConcreteReq request)
    {
        _entityQuery = request;
        _includeAll = request.Include.ToList();
        _keysAll = request.Keys.Select(FormatKeySet).ToList();
        SchedulePersist();
    }

    private void OnRunPanelProjectionRequestChanged(ProjectionQueryReq request)
    {
        _projectionQuery = request;
        _selectAll = request.Select.ToList();
        _keysAll = request.Keys.Select(FormatKeySet).ToList();
        SchedulePersist();
    }

    private void OnRunPanelRootRequestChanged(QueryReq request)
    {
        _rootQuery = request;
        EnsureRootQueryShape(_rootQuery);
        _selectAll = request.Select.ToList();
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

    private Task OnWorkbenchModeChanged(QueryWorkbenchRunMode mode)
    {
        if (_runConfig.RunMode == mode)
            return Task.CompletedTask;

        var route = _runConfig.Route;
        if (mode == QueryWorkbenchRunMode.RootQuery && _runConfig.RunMode != QueryWorkbenchRunMode.RootQuery)
            route = QueryRunPanel.EntityRouteToDynamicBase(route);
        else if (mode != QueryWorkbenchRunMode.RootQuery && _runConfig.RunMode == QueryWorkbenchRunMode.RootQuery) {
            var entityRoutes = EntityRoutesForSelectedHost().ToList();
            if (entityRoutes.Count > 0 && !entityRoutes.Contains(route, StringComparer.OrdinalIgnoreCase))
                route = entityRoutes[0];
        }

        return OnRunConfigurationChanged(_runConfig with { RunMode = mode, Route = route });
    }

    private IEnumerable<string> EntityRoutesForSelectedHost()
    {
        if (_runConfig.HostEndpoints.Count == 0 || string.IsNullOrWhiteSpace(_runConfig.SelectedHost))
            return [];

        foreach (var kvp in _runConfig.HostEndpoints) {
            if (string.Equals(
                QueryWorkbenchHostNormalization.NormalizeBaseUrl(kvp.Key),
                QueryWorkbenchHostNormalization.NormalizeBaseUrl(_runConfig.SelectedHost!),
                StringComparison.OrdinalIgnoreCase))
                return kvp.Value is { Count: > 0 } ? kvp.Value : [];
        }

        return [];
    }

    private Task OnRunConfigurationChanged(QueryWorkbenchRunConfiguration run)
    {
        if (_trackedRunMode.HasValue && _trackedRunMode.Value != run.RunMode)
            SyncSharedFieldsOnModeChange(_trackedRunMode.Value, run.RunMode);

        _trackedRunMode = run.RunMode;
        _runConfig = run;
        SchedulePersist();
        return Task.CompletedTask;
    }

    private void SyncSharedFieldsOnModeChange(QueryWorkbenchRunMode from, QueryWorkbenchRunMode to)
    {
        // Concrete ↔ Project share paging/where/sort/keys; Root keeps its own From/Joins shape.
        if (from == QueryWorkbenchRunMode.Query && to == QueryWorkbenchRunMode.QueryProject)
            CopySharedFields(_entityQuery, _projectionQuery);
        else if (from == QueryWorkbenchRunMode.QueryProject && to == QueryWorkbenchRunMode.Query)
            CopySharedFields(_projectionQuery, _entityQuery);
        else if (to == QueryWorkbenchRunMode.RootQuery && from is QueryWorkbenchRunMode.Query or QueryWorkbenchRunMode.QueryProject) {
            var source = from == QueryWorkbenchRunMode.Query ? (QueryRequestBase)_entityQuery : _projectionQuery;
            CopyPagingWhereSortToRoot(source, _rootQuery);
        }
        else if (from == QueryWorkbenchRunMode.RootQuery && to == QueryWorkbenchRunMode.Query)
            CopyPagingWhereSortFromRoot(_rootQuery, _entityQuery);
        else if (from == QueryWorkbenchRunMode.RootQuery && to == QueryWorkbenchRunMode.QueryProject)
            CopyPagingWhereSortFromRoot(_rootQuery, _projectionQuery);
    }

    private static QueryConcreteReq FromProjectionSharedFields(ProjectionQueryReq projection)
    {
        var entity = new QueryConcreteReq { Start = projection.Start, Amount = projection.Amount };
        CopySharedFields(projection, entity);
        return entity;
    }

    private static void CopySharedFields(ProjectionQueryReq source, QueryConcreteReq target)
    {
        target.Start = source.Start;
        target.Amount = source.Amount;
        target.Keys = CloneKeyRows(source.Keys);
        target.WhereClause = source.WhereClause;
        target.Include = source.Include.ToList();
        target.SortBy = source.SortBy.ToList();
        target.Options.TotalCountMode = source.Options.TotalCountMode;
        target.Options.IncludeFilterMode = source.Options.IncludeFilterMode;
    }

    private static void CopySharedFields(QueryConcreteReq source, ProjectionQueryReq target)
    {
        target.Start = source.Start;
        target.Amount = source.Amount;
        target.Keys = CloneKeyRows(source.Keys);
        target.WhereClause = source.WhereClause;
        target.Include = source.Include.ToList();
        target.SortBy = source.SortBy.ToList();
        target.Options.TotalCountMode = source.Options.TotalCountMode;
        target.Options.IncludeFilterMode = source.Options.IncludeFilterMode;
    }

    private static void CopyPagingWhereSortToRoot(QueryRequestBase source, QueryReq target)
    {
        target.Start = source.Start;
        target.Amount = source.Amount;
        target.WhereClause = source.WhereClause;
        target.SortBy = source.SortBy.ToList();
        target.Options.TotalCountMode = source switch {
            ProjectionQueryReq p => p.Options.TotalCountMode,
            QueryConcreteReq c => c.Options.TotalCountMode,
            _ => target.Options.TotalCountMode
        };
    }

    private static void CopyPagingWhereSortFromRoot(QueryReq source, QueryRequestBase target)
    {
        target.Start = source.Start;
        target.Amount = source.Amount;
        target.WhereClause = source.WhereClause;
        target.SortBy = source.SortBy.ToList();
        if (target is ProjectionQueryReq p)
            p.Options.TotalCountMode = source.Options.TotalCountMode;
        else if (target is QueryConcreteReq c)
            c.Options.TotalCountMode = source.Options.TotalCountMode;
    }

    private static QueryReq CreateDefaultRootQuery()
        => new() {
            Start = 0,
            Amount = 20,
            From = new FromClause { Alias = "o", EntityType = "" },
            Joins = [],
            Select = []
        };

    private static void EnsureRootQueryShape(QueryReq q)
    {
        q.Options ??= new();
        q.From ??= new FromClause();
        q.Joins ??= [];
        q.Select ??= [];
        if (string.IsNullOrWhiteSpace(q.From.Alias))
            q.From.Alias = "o";
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
