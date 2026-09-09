using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Api.Services.TypeConversion;
using Lyo.Common.Core.Caching;
using Lyo.Common.Core.Enums;
using Lyo.Hashing;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Builds cache tags for basic and projected query results. Keys stay in <see cref="QueryCacheKeyBuilder" />.</summary>
public static class QueryCacheTagBuilder
{
    private const int MaxProjectionShapeMemoEntries = 4096;

    /// <summary>Broad tag for every list-query cache entry (same as the SQL projection path and historical convention).</summary>
    public const string QueryScopeTag = "queries";

    /// <summary>Tag for SQL-level projected query cache entries.</summary>
    public const string QueryProjectScopeTag = "queryproject";

    /// <summary>
    /// Memoizes projection-shape tags; keys are normalized payloads (same string as pre-hash input). Bounded, because the payload is derived from the caller's select list. It
    /// used to be cleared wholesale on overflow, which sent every in-flight request back to SHA-1 at once.
    /// </summary>
    private static readonly BoundedCache<string, string> ProjShapeTagMemo = new(MaxProjectionShapeMemoEntries, StringComparer.Ordinal);

    /// <summary>
    /// Tag for all cached entries scoped to a root entity CLR type (e.g. <c>entity:acme.catalog.widget</c>). Keyed on <see cref="Type.FullName" /> rather than
    /// <see cref="Type.Name" />: two entities both called <c>Person</c> in different namespaces otherwise shared one tag, so a write to either invalidated both.
    /// </summary>
    public static string EntityTypeTag(Type entityClrType) => Lyo.Cache.Constants.Tags.EntityType(entityClrType);

    /// <summary>
    /// Stable segment for primary key values, in EF key property order (same order as <see cref="ITypeConversionService.GetPrimaryKeyValues{TEntity}" />). Each component is
    /// length-prefixed and rendered invariantly, so a value containing the separator cannot produce the same segment as a different key — <c>["a|b", "c"]</c> and
    /// <c>["a", "b|c"]</c> used to collide, letting one row's cache entry answer for another.
    /// </summary>
    public static string FormatPrimaryKeySegment(IReadOnlyList<object?> keyValues) => WhereClauseHelpers.FormatValueCanonical(keyValues);

    /// <summary>Tag for one entity instance: <c>entity:&lt;type&gt;:&lt;pkSegment&gt;</c>.</summary>
    public static string EntityInstanceTag(Type entityClrType, IReadOnlyList<object?> primaryKeyValues)
        => $"{EntityTypeTag(entityClrType)}:{FormatPrimaryKeySegment(primaryKeyValues)}";

    /// <summary>
    /// QueryProject-only tag: an include-derived or projected navigation touched this CLR type. Used when projected rows cannot carry <see cref="EntityInstanceTag" /> (e.g.
    /// single-field SQL projection returns scalars/lists). Patch invalidates this so related rows bust without using <see cref="EntityTypeTag" /> (which would clear unrelated list-query
    /// caches for the same type).
    /// </summary>
    public static string QueryProjectReferencedEntityTag(Type entityClrType) => $"qprefentity:{(entityClrType.FullName ?? entityClrType.Name).ToLowerInvariant()}";

    /// <summary>Fingerprint tag for the projection column set (select, computed, and zip shape). Uses SHA-1 of a normalized payload (not for security).</summary>
    public static string FormatProjShapeTag(IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs, IReadOnlyList<ComputedField> computedFields, bool zipSiblingCollectionSelections)
    {
        var parts = new List<string>();
        foreach (var spec in projectedFieldSpecs.OrderBy(s => s.NormalizedPath, StringComparer.OrdinalIgnoreCase))
            parts.Add("s:" + spec.NormalizedPath);

        foreach (var cf in computedFields.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            parts.Add($"c:{cf.Name}={cf.Template}");

        parts.Add("zip:" + zipSiblingCollectionSelections);
        var payload = string.Join('\u001e', parts);
        return ProjShapeTagMemo.GetOrAdd(
            payload, static p => {
                var hash = SHA1.HashData(Encoding.UTF8.GetBytes(p));
                return $"projshape:{HexEncoding.ToHexString(hash.AsSpan(), TextLetterCase.Lower)}";
            });
    }

    /// <summary>
    /// Tags for SQL-projected query pages: scope tags, <see cref="FormatProjShapeTag" />, root and cascade instance tags from projected row dictionaries, broad
    /// <c>entity:&lt;refType&gt;</c> tags for include-derived types, and <see cref="QueryProjectReferencedEntityTag" /> for patch invalidation when instance tags cannot be derived from
    /// row shape.
    /// </summary>
    public static string[] BuildProjectedSqlQueryTags<TDbModel>(
        IReadOnlyList<object?> projectedRows,
        DbContext context,
        ITypeConversionService typeConversion,
        IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs,
        IReadOnlyList<ComputedField> computedFields,
        bool zipSiblingCollectionSelections,
        IReadOnlyList<string>? includes,
        IReadOnlyList<Type> referencedIncludeEntityTypes)
        where TDbModel : class
    {
        var rootClr = typeof(TDbModel);
        var tagSet = new HashSet<string>(StringComparer.Ordinal) {
            QueryScopeTag,
            QueryProjectScopeTag,
            "entities",
            EntityTypeTag(rootClr),
            QueryProjectReferencedEntityTag(rootClr),
            FormatProjShapeTag(projectedFieldSpecs, computedFields, zipSiblingCollectionSelections)
        };

        foreach (var refType in referencedIncludeEntityTypes) {
            tagSet.Add(EntityTypeTag(refType));
            tagSet.Add(QueryProjectReferencedEntityTag(refType));
        }

        var rootEntityType = context.Model.FindEntityType(rootClr);
        if (rootEntityType is null)
            return tagSet.ToArray();

        var projectionChains = includes is { Count: > 0 } ? CompileIncludeNavigationChains(rootEntityType, includes) : null;
        foreach (var row in projectedRows) {
            var dict = AsReadOnlyStringDictionary(row);
            if (dict is null)
                continue;

            var rootPk = typeConversion.TryGetPrimaryKeyValuesFromProjectedDictionary(dict, rootClr, context);
            if (rootPk is not null)
                tagSet.Add(EntityInstanceTag(rootClr, rootPk));

            if (projectionChains is not null)
                AppendProjectionIncludeCascadeTags(tagSet, dict, projectionChains, rootEntityType, context, typeConversion);
        }

        return tagSet.ToArray();
    }

    /// <summary>
    /// Tags for a cached basic query page: <see cref="QueryScopeTag" />, <see cref="EntityTypeTag" />, one <see cref="EntityInstanceTag" /> per root row, and cascade
    /// <see cref="EntityInstanceTag" /> for every entity reached via the declared includes (including collection items). The filter tree and computed field templates are
    /// walked too, so a type the query only filters or formats on is still tagged and still busted when it is written.
    /// </summary>
    public static string[] BuildBasicQueryTags<TDbModel>(
        IReadOnlyList<TDbModel> rows,
        DbContext context,
        ITypeConversionService typeConversion,
        IReadOnlyList<string>? includes = null,
        Lyo.Query.Models.Common.WhereClause? whereClause = null,
        IReadOnlyList<ComputedField>? computedFields = null)
        where TDbModel : class
    {
        var entityType = typeof(TDbModel);
        var estimated = Math.Clamp(rows.Count * 3 + 8, 16, 8192);
        var tagSet = new HashSet<string>(estimated, StringComparer.Ordinal) { QueryScopeTag, EntityTypeTag(entityType) };
        var rootEntityType = context.Model.FindEntityType(entityType);
        AppendReferencedTypeTags(tagSet, rootEntityType, includes, whereClause, computedFields);
        var includeChains = includes is { Count: > 0 } ? CompileIncludeNavigationChains(rootEntityType, includes) : null;
        foreach (var row in rows) {
            tagSet.Add(EntityInstanceTag(entityType, typeConversion.GetPrimaryKeyValues(row, context)));
            if (includeChains is not null)
                AppendIncludeCascadeTagsCompiled(tagSet, row, includeChains, context, typeConversion);
        }

        return tagSet.ToArray();
    }

    /// <summary>
    /// Type-scoped tags for list queries when <see cref="Lyo.Cache.QueryCacheTagGranularity.Broad" /> is configured: <see cref="QueryScopeTag" />, <c>entities</c>, root
    /// <see cref="EntityTypeTag" />, and one <see cref="EntityTypeTag" /> per CLR type referenced by an include, the filter tree, or a computed field template (no per-row
    /// instance tags).
    /// </summary>
    public static string[] BuildBasicQueryTagsBroad<TContext, TDbModel>(
        TContext context,
        IEntityLoaderService loader,
        IReadOnlyList<string>? includes,
        Lyo.Query.Models.Common.WhereClause? whereClause = null,
        IReadOnlyList<ComputedField>? computedFields = null)
        where TContext : DbContext where TDbModel : class
    {
        var tagSet = new HashSet<string>(StringComparer.Ordinal) { QueryScopeTag, "entities", EntityTypeTag(typeof(TDbModel)) };
        if (includes is { Count: > 0 }) {
            foreach (var t in loader.GetReferencedTypes<TContext, TDbModel>(context, includes))
                tagSet.Add(EntityTypeTag(t));
        }

        AppendReferencedTypeTags(tagSet, context.Model.FindEntityType(typeof(TDbModel)), includes, whereClause, computedFields);
        return tagSet.ToArray();
    }

    /// <summary>
    /// Broad tags for SQL-projected pages: scope, <see cref="QueryProjectScopeTag" />, <c>entities</c>, <see cref="FormatProjShapeTag" />, and <see cref="EntityTypeTag" /> for
    /// root and include-referenced types only (no per-row or <see cref="QueryProjectReferencedEntityTag" /> tags).
    /// </summary>
    public static string[] BuildProjectedSqlQueryTagsBroad<TDbModel>(
        IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs,
        IReadOnlyList<ComputedField> computedFields,
        bool zipSiblingCollectionSelections,
        IReadOnlyList<Type> referencedIncludeEntityTypes)
        where TDbModel : class
    {
        var rootClr = typeof(TDbModel);
        var tagSet = new HashSet<string>(StringComparer.Ordinal) {
            QueryScopeTag,
            QueryProjectScopeTag,
            "entities",
            EntityTypeTag(rootClr),
            FormatProjShapeTag(projectedFieldSpecs, computedFields, zipSiblingCollectionSelections)
        };

        foreach (var refType in referencedIncludeEntityTypes)
            tagSet.Add(EntityTypeTag(refType));

        return tagSet.ToArray();
    }

    /// <summary>Broad tags for GET-by-key cache entries: <c>entities</c>, root type, and include-referenced types (no instance tags).</summary>
    public static string[] BuildSingleEntityGetCacheTagsBroad<TContext, TDbModel>(TContext context, IEntityLoaderService loader, IReadOnlyList<string> matIncludes)
        where TContext : DbContext where TDbModel : class
    {
        var tagSet = new HashSet<string>(StringComparer.Ordinal) { "entities", EntityTypeTag(typeof(TDbModel)) };
        if (matIncludes.Count > 0) {
            foreach (var t in loader.GetReferencedTypes<TContext, TDbModel>(context, matIncludes))
                tagSet.Add(EntityTypeTag(t));
        }

        return tagSet.ToArray();
    }

    /// <summary>Broad tags for PATCH/UPDATE refresh of a GET cache entry when includes are not applied (<c>entities</c> plus root type only).</summary>
    public static string[] BuildSingleEntityGetRootTypeTags<TDbModel>()
        where TDbModel : class
        => ["entities", EntityTypeTag(typeof(TDbModel))];

    /// <summary>
    /// Adds a type tag for every CLR type the query reaches beyond its root: the target of each declared include hop, and every navigation its filter and computed-field
    /// templates traverse.
    /// </summary>
    /// <remarks>
    /// Two gaps close here. A filter-only query used to tag only the root entity, so a query on <c>JobDefinition</c> filtering <c>JobRuns.CreatedBy</c> was never invalidated
    /// by a <c>JobRun</c> write. And include tags were derived from loaded navigation values, so an include whose navigation came back null contributed nothing and the entry
    /// survived a write that would have populated it.
    /// </remarks>
    /// <param name="tags">Destination tag set.</param>
    /// <param name="rootEntityType">EF metadata for the query root; nothing is added when null.</param>
    /// <param name="includes">Declared include paths.</param>
    /// <param name="whereClause">The filter tree.</param>
    /// <param name="computedFields">Computed field templates, whose <c>{Path}</c> placeholders are treated as referenced paths.</param>
    public static void AppendReferencedTypeTags(
        HashSet<string> tags,
        IEntityType? rootEntityType,
        IReadOnlyList<string>? includes,
        Lyo.Query.Models.Common.WhereClause? whereClause,
        IReadOnlyList<ComputedField>? computedFields)
    {
        if (rootEntityType is null)
            return;

        if (includes is { Count: > 0 }) {
            foreach (var include in includes)
                AppendPathTypeTags(tags, rootEntityType, include);
        }

        if (whereClause is not null) {
            foreach (var condition in WhereClauseHelpers.EnumerateConditions(whereClause))
                AppendPathTypeTags(tags, rootEntityType, condition.Field);
        }

        if (computedFields is { Count: > 0 }) {
            foreach (var computed in computedFields) {
                foreach (var path in ExtractTemplatePaths(computed.Template))
                    AppendPathTypeTags(tags, rootEntityType, path);
            }
        }
    }

    /// <summary>Walks a dotted path across navigations, tagging each target type. Stops at the first segment that is not a navigation (a scalar leaf, or an unknown name).</summary>
    private static void AppendPathTypeTags(HashSet<string> tags, IEntityType rootEntityType, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var current = rootEntityType;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            var nav = FindNavigationCaseInsensitive(current, segment);
            if (nav is null)
                return;

            current = nav.TargetEntityType;
            tags.Add(EntityTypeTag(current.ClrType));
        }
    }

    /// <summary>Pulls the <c>{Path}</c> placeholders out of a computed-field template and drops any <c>:format</c> suffix.</summary>
    private static IEnumerable<string> ExtractTemplatePaths(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
            yield break;

        var index = 0;
        while (index < template!.Length) {
            var open = template.IndexOf('{', index);
            if (open < 0)
                yield break;

            var close = template.IndexOf('}', open + 1);
            if (close < 0)
                yield break;

            var token = template.Substring(open + 1, close - open - 1);
            var colon = token.IndexOf(':');
            if (colon >= 0)
                token = token[..colon];

            if (token.Length > 0)
                yield return token.Trim();

            index = close + 1;
        }
    }

    /// <summary>Walks validated include paths and tags each referenced entity instance (duplicates removed).</summary>
    public static void AppendIncludeCascadeTags(HashSet<string> tags, object rootEntity, IReadOnlyList<string> includes, DbContext context, ITypeConversionService typeConversion)
    {
        var rootEntry = context.Entry(rootEntity);
        var chains = CompileIncludeNavigationChains(rootEntry.Metadata, includes);
        if (chains is not null)
            AppendIncludeCascadeTagsCompiled(tags, rootEntity, chains, context, typeConversion);
    }

    /// <summary>Resolves each include string to an EF navigation chain once per call, avoiding repeated string splits and O(n) navigation scans per hop.</summary>
    private static INavigation[][]? CompileIncludeNavigationChains(IEntityType? rootEntityType, IReadOnlyList<string> includes)
    {
        if (rootEntityType is null)
            return null;

        List<INavigation[]>? list = null;
        foreach (var include in includes) {
            if (string.IsNullOrWhiteSpace(include))
                continue;

            var segments = include.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                continue;

            var navigations = new INavigation[segments.Length];
            var type = rootEntityType;
            var ok = true;
            for (var i = 0; i < segments.Length; i++) {
                var nav = FindNavigationCaseInsensitive(type, segments[i]);
                if (nav is null) {
                    ok = false;
                    break;
                }

                navigations[i] = nav;
                type = nav.TargetEntityType;
            }

            if (!ok)
                continue;

            list ??= [];
            list.Add(navigations);
        }

        return list is null ? null : list.ToArray();
    }

    private static INavigation? FindNavigationCaseInsensitive(IEntityType type, string segment)
    {
        var direct = type.FindNavigation(segment);
        if (direct is not null)
            return direct;

        foreach (var n in type.GetNavigations()) {
            if (string.Equals(n.Name, segment, StringComparison.OrdinalIgnoreCase))
                return n;
        }

        return null;
    }

    private static void AppendIncludeCascadeTagsCompiled(HashSet<string> tags, object rootEntity, INavigation[][] chains, DbContext context, ITypeConversionService typeConversion)
    {
        foreach (var chain in chains)
            WalkIncludeChain(rootEntity, chain, 0);

        return;

        void WalkIncludeChain(object? current, INavigation[] segments, int index)
        {
            if (current is null || index >= segments.Length)
                return;

            var nav = segments[index];
            var entry = context.Entry(current);
            var navigation = entry.Navigation(nav.Name);
            // Do not call NavigationEntry.Load() here. QueryService uses NoTracking. EF often reports
            // IsLoaded == false even when Include() already populated the graph, so Load() caused N+1
            // SQL. Split-query settings only affect the main Include query shape, not this path.
            var val = navigation.CurrentValue;
            if (val is null)
                return;

            var isLast = index == segments.Length - 1;
            if (val is IEnumerable enumerable && val is not string && val is not byte[]) {
                foreach (var item in enumerable) {
                    if (item is null)
                        continue;

                    TryAddEntityTag(item);
                    if (!isLast)
                        WalkIncludeChain(item, segments, index + 1);
                }
            }
            else {
                TryAddEntityTag(val);
                if (!isLast)
                    WalkIncludeChain(val, segments, index + 1);
            }
        }

        void TryAddEntityTag(object entity)
        {
            var ent = context.Entry(entity);
            if (ent.Metadata.FindPrimaryKey() is null)
                return;

            tags.Add(EntityInstanceTag(ent.Metadata.ClrType, typeConversion.GetPrimaryKeyValues(entity, context)));
        }
    }

    private static void AppendProjectionIncludeCascadeTags(
        HashSet<string> tags,
        IReadOnlyDictionary<string, object?> rootRow,
        INavigation[][] chains,
        IEntityType rootEntityType,
        DbContext context,
        ITypeConversionService typeConversion)
    {
        foreach (var chain in chains)
            WalkProjectionInclude(rootRow, rootEntityType, chain, 0);

        return;

        void WalkProjectionInclude(object? node, IEntityType sourceEntityType, INavigation[] segments, int index)
        {
            if (node is null || index >= segments.Length)
                return;

            var navigation = segments[index];
            var segment = navigation.Name;
            var targetType = navigation.TargetEntityType;
            var isLast = index == segments.Length - 1;
            var rowDict = AsReadOnlyStringDictionary(node);
            if (rowDict is null)
                return;

            if (!TryGetProjectionNavigationValue(rowDict, segment, out var raw))
                return;

            if (navigation.IsCollection) {
                if (raw is not IEnumerable enumerable || raw is string || raw is byte[])
                    return;

                foreach (var el in enumerable) {
                    var childDict = AsReadOnlyStringDictionary(el);
                    if (childDict is null)
                        continue;

                    TryAddDictEntityTag(childDict, targetType);
                    if (!isLast)
                        WalkProjectionInclude(childDict, targetType, segments, index + 1);
                }
            }
            else {
                if (raw is IReadOnlyDictionary<string, object?> refDict) {
                    TryAddDictEntityTag(refDict, targetType);
                    if (!isLast)
                        WalkProjectionInclude(refDict, targetType, segments, index + 1);
                }
                else if (raw is IDictionary<string, object?> idRef) {
                    var rd = new Dictionary<string, object?>(idRef, StringComparer.OrdinalIgnoreCase);
                    TryAddDictEntityTag(rd, targetType);
                    if (!isLast)
                        WalkProjectionInclude(rd, targetType, segments, index + 1);
                }
            }
        }

        void TryAddDictEntityTag(IReadOnlyDictionary<string, object?> rowDict, IEntityType entityType)
        {
            var clr = entityType.ClrType;
            var pk = typeConversion.TryGetPrimaryKeyValuesFromProjectedDictionary(rowDict, clr, context);
            if (pk is not null)
                tags.Add(EntityInstanceTag(clr, pk));
        }
    }

    private static IReadOnlyDictionary<string, object?>? AsReadOnlyStringDictionary(object? node)
        => node switch {
            IReadOnlyDictionary<string, object?> d => d,
            IDictionary<string, object?> mutable => new Dictionary<string, object?>(mutable, StringComparer.OrdinalIgnoreCase),
            var _ => null
        };

    private static bool TryGetProjectionNavigationValue(IReadOnlyDictionary<string, object?> dict, string segment, out object? value)
    {
        if (dict.TryGetValue(segment, out value))
            return true;

        foreach (var kv in dict) {
            if (string.Equals(kv.Key, segment, StringComparison.OrdinalIgnoreCase)) {
                value = kv.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}