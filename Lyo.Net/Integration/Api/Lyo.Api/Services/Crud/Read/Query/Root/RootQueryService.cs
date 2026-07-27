using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Cache;
using Lyo.Common.Enums;
using Lyo.Formatter;
using Lyo.Metrics;
using Lyo.Metrics.Models;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;

namespace Lyo.Api.Services.Crud.Read.Query.Root;

/// <summary>
/// Root <see cref="QueryReq" />: arbitrary From/Joins (EF Join/GroupJoin on ON columns) + sparse Select.
/// Not navigation-based — that is <c>/QueryProject</c>.
/// </summary>
public interface IRootQueryService<TContext>
    where TContext : DbContext
{
    Task<ProjectedQueryRes<object?>> QueryAsync(QueryReq request, RootQueryEntityRegistry registry, CancellationToken ct = default);
}

public sealed class RootQueryService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    IWhereClauseService filterService,
    ICacheService cache,
    QueryOptions queryOptions,
    IProjectionService projectionService,
    IFormatterService? formatterService = null,
    ILogger<RootQueryService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : IRootQueryService<TContext>
    where TContext : DbContext
{
    private const string Operation = "query_root";
    private const string Endpoint = "queryroot";

    private static readonly ConcurrentDictionary<string, RootQueryShapePlan> PlanCache = new(StringComparer.Ordinal);

    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;

    public async Task<ProjectedQueryRes<object?>> QueryAsync(QueryReq request, RootQueryEntityRegistry registry, CancellationToken ct = default)
    {
        var fromEntityName = string.IsNullOrWhiteSpace(request.From.EntityType) ? "unknown" : request.From.EntityType.Trim();
        _metrics.IncrementCounter("api.crud.requests", 1, CrudTags(fromEntityName));
        using var timer = _metrics.StartTimer("api.crud.duration", CrudTags(fromEntityName));

        try {
            if (request.ComputedFields.Count > 0)
                RootQueryComputedFields.EnsureSelectIncludesComputedDependencies(request, projectionService, formatterService);

            var validationErrors = RootQueryValidator.Validate(request, registry);
            validationErrors.AddRange(QueryPagingBoundsValidator.Validate(request, queryOptions, queryOptions.MaxPageSize));
            validationErrors.AddRange(RootQueryComputedFields.ValidateGuardrails(request, queryOptions));
            if (request.ComputedFields.Count > 0)
                validationErrors.AddRange(projectionService.ValidateComputedFieldTemplates(request.ComputedFields));

            if (validationErrors.Count > 0) {
                _metrics.IncrementCounter("api.crud.failure", 1, CrudTags(fromEntityName));
                return ResultFactory.ProjectedQueryFailure<object?>(
                    request,
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(ApiErrorCodes.InvalidQuery)
                        .WithMessage("Invalid root query.")
                        .AddErrors(validationErrors)
                        .Build());
            }

            if (!registry.TryGet(request.From.EntityType, out var fromEntry)) {
                _metrics.IncrementCounter("api.crud.failure", 1, CrudTags(fromEntityName));
                return ResultFactory.ProjectedQueryFailure<object?>(request, LyoProblemDetails.FromCode(ApiErrorCodes.InvalidQuery, "From entity type not found.", DateTime.UtcNow));
            }

            fromEntityName = fromEntry.ClrType.Name;
            var planStart = Stopwatch.GetTimestamp();
            var plan = PlanCache.GetOrAdd(BuildShapeKey(request), _ => BuildPlan(request, registry, fromEntry));
            TrackPhase(fromEntityName, "plan_resolve", planStart);

            var cacheKey = QueryCacheKeyBuilder.AppendProjectedShapeSuffix(
                QueryCacheKeyBuilder.BuildRootQuery(request, typeof(TContext).Name),
                request.Options.ZipSiblingCollectionSelections);

            async Task<(ProjectedQueryRes<object?>? projected, string[]? tags)> BuildEntryAsync(CancellationToken token)
            {
                await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
                var result = await ExecuteCoreAsync(context, request, registry, fromEntry, plan, fromEntityName, token).ConfigureAwait(false);
                return (result, BuildTags(registry, plan));
            }

            ProjectedQueryRes<object?> result;
            try {
                if (queryOptions.CacheQueryResultsAsUtf8Payload) {
                    result = (await cache.GetOrSetPayloadAsync(cacheKey, BuildEntryAsync, token: ct).ConfigureAwait(false))!;
                    _metrics.IncrementCounter("api.queryroot.cache_path", 1, [("entity", fromEntityName), ("mode", "payload")]);
                }
                else {
                    result = (await cache.GetOrSetAsync(cacheKey, BuildEntryAsync, token: ct).ConfigureAwait(false))!;
                    _metrics.IncrementCounter("api.queryroot.cache_path", 1, [("entity", fromEntityName), ("mode", "object")]);
                }
            }
            catch (Exception ex) {
                logger?.LogWarning(ex, "Root query cache failed; executing without cache");
                _metrics.IncrementCounter("api.queryroot.cache_path", 1, [("entity", fromEntityName), ("mode", "bypass_error")]);
                await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                result = await ExecuteCoreAsync(context, request, registry, fromEntry, plan, fromEntityName, ct).ConfigureAwait(false);
            }

            if (result.IsSuccess) {
                _metrics.IncrementCounter("api.crud.success", 1, CrudTags(fromEntityName));
                _metrics.RecordGauge("api.crud.result_count", result.Items?.Count ?? 0, CrudTags(fromEntityName));
            }
            else
                _metrics.IncrementCounter("api.crud.failure", 1, CrudTags(fromEntityName));

            return result;
        }
        catch (OperationCanceledException) {
            _metrics.IncrementCounter("api.crud.cancelled", 1, CrudTags(fromEntityName));
            throw;
        }
        catch {
            _metrics.IncrementCounter("api.crud.failure", 1, CrudTags(fromEntityName));
            throw;
        }
    }

    private async Task<ProjectedQueryRes<object?>> ExecuteCoreAsync(
        TContext context,
        QueryReq request,
        RootQueryEntityRegistry registry,
        RootQueryEntityEntry fromEntry,
        RootQueryShapePlan plan,
        string fromEntityName,
        CancellationToken ct)
    {
        var scopeStart = Stopwatch.GetTimestamp();
        var fromSet = GetDbSetQueryable(context, fromEntry.ClrType);
        fromSet = ApplySourceScope(fromSet, fromEntry.ClrType, request.From.Query);
        fromSet = ApplyOuterWhereAndSort(fromSet, fromEntry.ClrType, request);
        TrackPhase(fromEntityName, "source_scope_sort", scopeStart);

        var start = request.Start ?? 0;
        var amount = request.Amount ?? queryOptions.DefaultPageSize;

        int? total = null;
        if (request.Options.TotalCountMode == QueryTotalCountMode.Exact) {
            var countStart = Stopwatch.GetTimestamp();
            total = await CountAsync(fromSet, fromEntry.ClrType, ct).ConfigureAwait(false);
            TrackPhase(fromEntityName, "exact_count", countStart);
        }

        // Apply join scopes from the live request (not the shape-plan cache — scopes vary per call).
        var scopedJoins = new List<IQueryable>(plan.Joins.Count);
        for (var ji = 0; ji < plan.Joins.Count; ji++) {
            var joinPlan = plan.Joins[ji];
            if (!registry.TryGet(joinPlan.EntityTypeName, out var joinEntry))
                throw new InvalidQueryException($"Join entity '{joinPlan.EntityTypeName}' not in registry.");

            var joinSet = GetDbSetQueryable(context, joinEntry.ClrType);
            var liveScope = ji < request.Joins.Count ? request.Joins[ji].Query : joinPlan.SourceQuery;
            joinSet = ApplySourceScope(joinSet, joinEntry.ClrType, liveScope);
            scopedJoins.Add(joinSet);
        }

        var joinStart = Stopwatch.GetTimestamp();
        var projectedRows = await RootQueryJoinExecutor
            .ExecuteAsync(fromSet, fromEntry.ClrType, start, amount, scopedJoins, plan, ct)
            .ConfigureAwait(false);
        TrackPhase(fromEntityName, "join_execute", joinStart);

        // SQL LEFT JOIN fans out; collapse to one item per From row (amount/start are From-side).
        var collapseStart = Stopwatch.GetTimestamp();
        var items = CollapseFanOutToFromItems(projectedRows, plan);
        TrackPhase(fromEntityName, "fan_out_collapse", collapseStart);
        _metrics.IncrementCounter(
            "api.queryroot.join_fanout",
            projectedRows.Count,
            [("entity", fromEntityName), ("joins", plan.Joins.Count.ToString())]);

        if (request.ComputedFields.Count > 0) {
            var computedStart = Stopwatch.GetTimestamp();
            items = RootQueryComputedFields.Apply(items, request.ComputedFields, plan, projectionService, formatterService).ToList();
            TrackPhase(fromEntityName, "computed_fields", computedStart);
        }

        var hasMore = request.Options.TotalCountMode == QueryTotalCountMode.Exact && total.HasValue
            ? start + items.Count < total.Value
            : items.Count >= amount;

        return ResultFactory.ProjectedQuerySuccess<object?>(request, items, start, amount, total, hasMore, entityTypes: plan.EntityTypeNames);
    }

    private void TrackPhase(string entity, string phase, long startTimestamp)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        _metrics.IncrementCounter("api.query.phase_ms", elapsedMs, [("entity", entity), ("endpoint", Endpoint), ("phase", phase)]);
    }

    private IEnumerable<(string, string)> CrudTags(string fromEntityName)
        => [
            ("operation", Operation), ("context", typeof(TContext).Name), ("database_type", fromEntityName),
            ("is_bulk", "false")
        ];

    /// <summary>
    /// Rows are <c>[FromPk, ...SelectSpecs]</c>. Group by FromPk; join columns become arrays of bags
    /// (and chained joins nest under the parent join bag).
    /// </summary>
    private static List<object?> CollapseFanOutToFromItems(List<object?[]> rows, RootQueryShapePlan plan)
    {
        if (rows.Count == 0)
            return [];

        if (plan.Joins.Count == 0) {
            // No fan-out; strip FromPk and map.
            var flat = new List<object?>(rows.Count);
            foreach (var row in rows)
                flat.Add(MapSingleRow(row.AsSpan(1), plan));
            return flat;
        }

        var fromSpecIndexes = new List<int>();
        for (var i = 0; i < plan.SelectSpecs.Count; i++) {
            if (plan.SelectSpecs[i].IsFromSide)
                fromSpecIndexes.Add(i);
        }

        // Parent join alias for nesting: join whose ON left is that alias.
        var childJoinsByParent = new Dictionary<string, List<RootQueryJoinPlan>>(StringComparer.OrdinalIgnoreCase);
        var rootJoins = new List<RootQueryJoinPlan>();
        foreach (var j in plan.Joins) {
            var left = j.On[0].LeftAlias;
            if (string.Equals(left, plan.FromAlias, StringComparison.OrdinalIgnoreCase))
                rootJoins.Add(j);
            else {
                if (!childJoinsByParent.TryGetValue(left, out var list)) {
                    list = [];
                    childJoinsByParent[left] = list;
                }

                list.Add(j);
            }
        }

        var items = new List<object?>();
        foreach (var group in rows.GroupBy(r => r[0])) {
            var groupRows = group.ToList();
            var root = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var first = groupRows[0];
            foreach (var si in fromSpecIndexes)
                root[plan.SelectSpecs[si].PropertyName] = first[si + 1];

            foreach (var join in rootJoins)
                root[join.ResultName] = BuildJoinBagList(groupRows, plan, join, childJoinsByParent);

            if (plan.SelectSpecs.Count == 1 && plan.SelectSpecs[0].IsFromSide)
                items.Add(root.Values.FirstOrDefault());
            else
                items.Add(root);
        }

        return items;
    }

    private static List<object?> BuildJoinBagList(
        List<object?[]> groupRows,
        RootQueryShapePlan plan,
        RootQueryJoinPlan join,
        IReadOnlyDictionary<string, List<RootQueryJoinPlan>> childJoinsByParent)
    {
        var joinSpecIndexes = new List<int>();
        for (var i = 0; i < plan.SelectSpecs.Count; i++) {
            if (string.Equals(plan.SelectSpecs[i].Alias, join.Alias, StringComparison.OrdinalIgnoreCase))
                joinSpecIndexes.Add(i);
        }

        var bags = new List<object?>();
        foreach (var row in groupRows) {
            // Skip left-join miss rows (all selected join props null) when there are join selects
            if (joinSpecIndexes.Count > 0 && joinSpecIndexes.All(i => row[i + 1] is null))
                continue;

            var bag = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var si in joinSpecIndexes)
                bag[plan.SelectSpecs[si].PropertyName] = row[si + 1];

            if (childJoinsByParent.TryGetValue(join.Alias, out var children)) {
                foreach (var child in children) {
                    // One child bag per fan-out row (chained join)
                    var childList = BuildJoinBagList([row], plan, child, childJoinsByParent);
                    bag[child.ResultName] = childList.Count switch {
                        0 => null,
                        1 => childList[0],
                        _ => childList
                    };
                }
            }

            if (bag.Count > 0)
                bags.Add(bag);
        }

        // Deduplicate identical bags (same contact repeated if somehow duplicated)
        return DeduplicateBags(bags);
    }

    private static List<object?> DeduplicateBags(List<object?> bags)
    {
        if (bags.Count <= 1)
            return bags;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<object?>();
        foreach (var bag in bags) {
            var key = System.Text.Json.JsonSerializer.Serialize(bag);
            if (seen.Add(key))
                unique.Add(bag);
        }

        return unique;
    }

    private static object? MapSingleRow(ReadOnlySpan<object?> selectValues, RootQueryShapePlan plan)
    {
        if (plan.SelectSpecs.Count == 1)
            return selectValues[0];

        var root = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < plan.SelectSpecs.Count; i++)
            root[plan.SelectSpecs[i].PropertyName] = selectValues[i];
        return root;
    }

    private static IQueryable GetDbSetQueryable(DbContext context, Type clrType)
    {
        var setMethod = typeof(DbContext).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == nameof(DbContext.Set) && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
        return (IQueryable)setMethod.MakeGenericMethod(clrType).Invoke(context, null)!;
    }

    private IQueryable ApplySourceScope(IQueryable source, Type clrType, SourceQueryScope? scope)
    {
        if (scope?.WhereClause is null && (scope?.Keys.Count ?? 0) == 0)
            return source;

        var method = typeof(RootQueryService<TContext>).GetMethod(nameof(ApplySourceScopeGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(clrType);
        return (IQueryable)method.Invoke(this, [source, scope])!;
    }

    private IQueryable<TEntity> ApplySourceScopeGeneric<TEntity>(IQueryable source, SourceQueryScope? scope)
        where TEntity : class
    {
        var q = (IQueryable<TEntity>)source;
        if (scope?.WhereClause != null)
            q = filterService.ApplyWhereClause(q, scope.WhereClause);
        return q;
    }

    private IQueryable ApplyOuterWhereAndSort(IQueryable source, Type clrType, QueryReq request)
    {
        var method = typeof(RootQueryService<TContext>).GetMethod(nameof(ApplyOuterWhereAndSortGeneric), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(clrType);
        return (IQueryable)method.Invoke(this, [source, request])!;
    }

    private IQueryable<TEntity> ApplyOuterWhereAndSortGeneric<TEntity>(IQueryable source, QueryReq request)
        where TEntity : class
    {
        var q = (IQueryable<TEntity>)source;
        var where = StripAliasPrefix(request.WhereClause, request.From.Alias);
        if (where != null)
            q = filterService.ApplyWhereClause(q, where);

        var ordered = false;
        foreach (var sort in request.SortBy.OrderBy(s => s.Priority ?? 0)) {
            var propName = StripAliasPrefix(sort.PropertyName, request.From.Alias);
            q = ApplySort(q, propName, sort.Direction ?? SortDirection.Desc, ordered);
            ordered = true;
        }

        return q;
    }

    private static WhereClause? StripAliasPrefix(WhereClause? clause, string alias)
    {
        if (clause is null)
            return null;

        return clause switch {
            ConditionClause c => new ConditionClause(StripAliasPrefix(c.Field, alias), c.Comparison, c.Value, c.Description) {
                SubClause = StripAliasPrefix(c.SubClause, alias)
            },
            GroupClause g => new GroupClause(g.Operator, g.Children.Select(ch => StripAliasPrefix(ch, alias)!).ToList(), g.Description) {
                SubClause = StripAliasPrefix(g.SubClause, alias)
            },
            _ => clause
        };
    }

    private static string StripAliasPrefix(string path, string alias)
    {
        var prefix = alias + ".";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? path[prefix.Length..] : path;
    }

    private static IQueryable<TEntity> ApplySort<TEntity>(IQueryable<TEntity> q, string propertyName, SortDirection direction, bool alreadyOrdered)
    {
        var param = Expression.Parameter(typeof(TEntity), "e");
        var prop = Expression.PropertyOrField(param, propertyName);
        var keySelector = Expression.Lambda(prop, param);
        // Do not use `q is IOrderedQueryable` — EF DbSet implements that interface even before OrderBy.
        var methodName = (alreadyOrdered, direction) switch {
            (false, SortDirection.Asc) => nameof(Queryable.OrderBy),
            (false, _) => nameof(Queryable.OrderByDescending),
            (true, SortDirection.Asc) => nameof(Queryable.ThenBy),
            (true, _) => nameof(Queryable.ThenByDescending)
        };

        var result = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), prop.Type)
            .Invoke(null, [q, keySelector])!;
        return (IQueryable<TEntity>)result;
    }

    private static async Task<int> CountAsync(IQueryable source, Type clrType, CancellationToken ct)
    {
        var method = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .First(m => m.Name == "CountAsync" && m.GetParameters().Length == 2)
            .MakeGenericMethod(clrType);
        var task = (Task)method.Invoke(null, [source, ct])!;
        await task.ConfigureAwait(false);
        return (int)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static RootQueryShapePlan BuildPlan(QueryReq request, RootQueryEntityRegistry registry, RootQueryEntityEntry fromEntry)
    {
        var fromAlias = request.From.Alias.Trim();
        var aliases = new Dictionary<string, RootQueryEntityEntry>(StringComparer.OrdinalIgnoreCase) { [fromAlias] = fromEntry };
        var joins = new List<RootQueryJoinPlan>();
        foreach (var join in request.Joins) {
            registry.TryGet(join.EntityType, out var joinEntry);
            var joinAlias = join.Alias.Trim();
            aliases[joinAlias] = joinEntry!;

            var onPlans = join.On.Select(o => {
                var (fa, fp) = Split(o.From);
                var (ta, tp) = Split(o.To);
                // Normalize so Right is the join being added when possible
                string leftAlias, rightAlias;
                PropertyInfo leftProp, rightProp;
                if (string.Equals(ta, joinAlias, StringComparison.OrdinalIgnoreCase)) {
                    leftAlias = fa;
                    rightAlias = ta;
                    aliases[fa].TryGetProperty(fp, out leftProp!);
                    aliases[ta].TryGetProperty(tp, out rightProp!);
                }
                else if (string.Equals(fa, joinAlias, StringComparison.OrdinalIgnoreCase)) {
                    leftAlias = ta;
                    rightAlias = fa;
                    aliases[ta].TryGetProperty(tp, out leftProp!);
                    aliases[fa].TryGetProperty(fp, out rightProp!);
                }
                else {
                    leftAlias = fa;
                    rightAlias = ta;
                    aliases[fa].TryGetProperty(fp, out leftProp!);
                    aliases[ta].TryGetProperty(tp, out rightProp!);
                }

                return new RootQueryOnPlan(leftAlias, leftProp, rightAlias, rightProp);
            }).ToList();

            joins.Add(
                new(
                    joinAlias,
                    join.EntityType,
                    string.IsNullOrWhiteSpace(join.As) ? joinAlias : join.As.Trim(),
                    join.Type,
                    onPlans,
                    join.Query));
        }

        var specs = new List<RootQuerySelectSpec>();
        foreach (var path in request.Select) {
            var (alias, propName) = Split(path);
            var entry = aliases[alias];
            entry.TryGetProperty(propName, out var prop);
            var isFrom = string.Equals(alias, fromAlias, StringComparison.OrdinalIgnoreCase);
            string? joinResultName = null;
            if (!isFrom) {
                var jp = joins.First(j => string.Equals(j.Alias, alias, StringComparison.OrdinalIgnoreCase));
                joinResultName = jp.ResultName;
            }

            specs.Add(new(path, alias, propName, prop!, isFrom, joinResultName));
        }

        var pk = fromEntry.PrimaryKey?.Properties.FirstOrDefault()?.PropertyInfo
            ?? fromEntry.ClrType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"From entity '{fromEntry.ClrType.Name}' has no primary key for fan-out collapse.");

        var typeNames = new[] { fromEntry.ClrType.Name }.Concat(joins.Select(j => j.EntityTypeName)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        return new(fromAlias, pk, specs, joins, typeNames);
    }

    private static (string Alias, string Property) Split(string path)
    {
        var i = path.IndexOf('.');
        return (path[..i].Trim(), path[(i + 1)..].Trim());
    }

    private static string BuildShapeKey(QueryReq request)
    {
        var sb = new StringBuilder();
        sb.Append(request.From.EntityType).Append('|').Append(request.From.Alias);
        foreach (var j in request.Joins) {
            sb.Append(';').Append(j.Type).Append(':').Append(j.EntityType).Append(':').Append(j.Alias).Append(':').Append(j.As);
            foreach (var o in j.On)
                sb.Append('#').Append(o.From).Append('=').Append(o.To);
        }

        foreach (var s in request.Select.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            sb.Append('|').Append(s.Trim().ToLowerInvariant());

        foreach (var cf in request.ComputedFields.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            sb.Append('~').Append(cf.Name).Append('=').Append(cf.Template);

        return sb.ToString();
    }

    private static string[] BuildTags(RootQueryEntityRegistry registry, RootQueryShapePlan plan)
    {
        var tags = new HashSet<string>(StringComparer.Ordinal) {
            QueryCacheTagBuilder.QueryScopeTag,
            "queryroot",
            "entities"
        };
        foreach (var name in plan.EntityTypeNames) {
            if (registry.TryGet(name, out var e))
                tags.Add(QueryCacheTagBuilder.EntityTypeTag(e.ClrType));
        }

        return tags.ToArray();
    }
}
