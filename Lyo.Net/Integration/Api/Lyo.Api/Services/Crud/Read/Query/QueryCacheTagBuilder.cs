using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Api.Services.TypeConversion;
using Lyo.Query.Models.Common.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Builds cache tags for basic and projected query results. Keys remain in <see cref="QueryCacheKeyBuilder" />.</summary>
public static class QueryCacheTagBuilder
{
    /// <summary>Broad tag for all list-query cache entries (same as SQL projection path and historical convention).</summary>
    public const string QueryScopeTag = "queries";

    /// <summary>Tag for SQL-level projected query cache entries.</summary>
    public const string QueryProjectScopeTag = "queryproject";

    /// <summary>Tag for all cached entries scoped to a root entity CLR type (e.g. <c>entity:widget</c>).</summary>
    public static string EntityTypeTag(Type entityClrType)
        => $"entity:{entityClrType.Name.ToLowerInvariant()}";

    /// <summary>
    /// Stable segment for primary key values (composite: <c>|</c>-joined in EF key property order, same as <see cref="ITypeConversionService.GetPrimaryKeyValues{TEntity}" />).
    /// </summary>
    public static string FormatPrimaryKeySegment(IReadOnlyList<object?> keyValues)
        => string.Join("|", keyValues.Select(k => k?.ToString() ?? "null"));

    /// <summary>Tag for one entity instance: <c>entity:&lt;type&gt;:&lt;pkSegment&gt;</c>.</summary>
    public static string EntityInstanceTag(Type entityClrType, IReadOnlyList<object?> primaryKeyValues)
        => $"{EntityTypeTag(entityClrType)}:{FormatPrimaryKeySegment(primaryKeyValues)}";

    /// <summary>
    /// QueryProject-only tag: an include-derived or projected navigation touched this CLR type. Used when projected rows cannot carry
    /// <see cref="EntityInstanceTag" /> (e.g. single-field SQL projection returns scalars/lists). Patch invalidates this so related rows bust
    /// without using <see cref="EntityTypeTag" /> (which would clear unrelated list-query caches for the same type).
    /// </summary>
    public static string QueryProjectReferencedEntityTag(Type entityClrType)
        => $"qprefentity:{entityClrType.Name.ToLowerInvariant()}";

    /// <summary>
    /// Fingerprint tag for the projection column set (select + computed + zip shape). Uses SHA-1 of a normalized payload (not for security).
    /// </summary>
    public static string FormatProjShapeTag(
        IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs,
        IReadOnlyList<ComputedField> computedFields,
        bool zipSiblingCollectionSelections)
    {
        var parts = new List<string>();
        foreach (var spec in projectedFieldSpecs.OrderBy(s => s.NormalizedPath, StringComparer.OrdinalIgnoreCase))
            parts.Add("s:" + spec.NormalizedPath);

        foreach (var cf in computedFields.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            parts.Add($"c:{cf.Name}={cf.Template}");

        parts.Add("zip:" + zipSiblingCollectionSelections);
        var payload = string.Join('\u001e', parts);
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(payload));
        return $"projshape:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Tags for SQL-projected query pages: scope tags, <see cref="FormatProjShapeTag" />, root and cascade instance tags from projected row dictionaries,
    /// broad <c>entity:&lt;refType&gt;</c> tags for include-derived types, and <see cref="QueryProjectReferencedEntityTag" /> for patch invalidation
    /// when instance tags cannot be derived from row shape.
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
            FormatProjShapeTag(projectedFieldSpecs, computedFields, zipSiblingCollectionSelections),
        };

        foreach (var refType in referencedIncludeEntityTypes) {
            tagSet.Add(EntityTypeTag(refType));
            tagSet.Add(QueryProjectReferencedEntityTag(refType));
        }

        var rootEntityType = context.Model.FindEntityType(rootClr);
        if (rootEntityType is null)
            return tagSet.ToArray();

        foreach (var row in projectedRows) {
            var dict = AsReadOnlyStringDictionary(row);
            if (dict is null)
                continue;

            var rootPk = typeConversion.TryGetPrimaryKeyValuesFromProjectedDictionary(dict, rootClr, context);
            if (rootPk is not null)
                tagSet.Add(EntityInstanceTag(rootClr, rootPk));

            if (includes is { Count: > 0 })
                AppendProjectionIncludeCascadeTags(tagSet, dict, includes, rootEntityType, context, typeConversion);
        }

        return tagSet.ToArray();
    }

    /// <summary>
    /// Tags for a cached basic query page: <see cref="QueryScopeTag"/>, <see cref="EntityTypeTag" />, one <see cref="EntityInstanceTag" /> per root row,
    /// and cascade <see cref="EntityInstanceTag" /> for every entity reached via <paramref name="includes" /> (including collection items).
    /// </summary>
    public static string[] BuildBasicQueryTags<TDbModel>(
        IReadOnlyList<TDbModel> rows,
        DbContext context,
        ITypeConversionService typeConversion,
        IReadOnlyList<string>? includes = null)
        where TDbModel : class
    {
        var entityType = typeof(TDbModel);
        var tagSet = new HashSet<string> { QueryScopeTag, EntityTypeTag(entityType) };
        foreach (var row in rows) {
            tagSet.Add(EntityInstanceTag(entityType, typeConversion.GetPrimaryKeyValues(row, context)));
            if (includes is { Count: > 0 })
                AppendIncludeCascadeTags(tagSet, row, includes, context, typeConversion);
        }

        return tagSet.ToArray();
    }

    /// <summary>Walks validated include paths and tags each referenced entity instance (deduped).</summary>
    public static void AppendIncludeCascadeTags(
        HashSet<string> tags,
        object rootEntity,
        IReadOnlyList<string> includes,
        DbContext context,
        ITypeConversionService typeConversion)
    {
        foreach (var include in includes) {
            if (string.IsNullOrWhiteSpace(include))
                continue;

            var segments = include.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                continue;

            WalkIncludePath(rootEntity, segments, 0);
        }

        return;

        void WalkIncludePath(object? current, IReadOnlyList<string> segments, int index)
        {
            if (current is null || index >= segments.Count)
                return;

            var navName = segments[index];
            var entry = context.Entry(current);
            var navMeta = entry.Metadata.GetNavigations().FirstOrDefault(n => string.Equals(n.Name, navName, StringComparison.OrdinalIgnoreCase));
            if (navMeta is null)
                return;

            var navigation = entry.Navigation(navMeta.Name);
            // Do not call NavigationEntry.Load() here. QueryService uses NoTracking; EF often reports
            // IsLoaded == false even when Include() already populated the graph, so Load() caused N+1
            // SQL (split-query settings only affect the main Include query shape, not this path).
            var val = navigation.CurrentValue;
            if (val is null)
                return;
            var isLast = index == segments.Count - 1;
            if (val is IEnumerable enumerable && val is not string) {
                foreach (var item in enumerable) {
                    if (item is null)
                        continue;

                    TryAddEntityTag(item);
                    if (!isLast)
                        WalkIncludePath(item, segments, index + 1);
                }
            }
            else if (val is not null) {
                TryAddEntityTag(val);
                if (!isLast)
                    WalkIncludePath(val, segments, index + 1);
            }
        }

        void TryAddEntityTag(object entity)
        {
            var entry = context.Entry(entity);
            if (entry.Metadata.FindPrimaryKey() is null)
                return;

            tags.Add(EntityInstanceTag(entry.Metadata.ClrType, typeConversion.GetPrimaryKeyValues(entity, context)));
        }
    }

    private static void AppendProjectionIncludeCascadeTags(
        HashSet<string> tags,
        IReadOnlyDictionary<string, object?> rootRow,
        IReadOnlyList<string> includes,
        IEntityType rootEntityType,
        DbContext context,
        ITypeConversionService typeConversion)
    {
        foreach (var include in includes) {
            if (string.IsNullOrWhiteSpace(include))
                continue;

            var segments = include.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                continue;

            WalkProjectionInclude(rootRow, rootEntityType, segments, 0);
        }

        return;

        void WalkProjectionInclude(
            object? node,
            IEntityType sourceEntityType,
            IReadOnlyList<string> segments,
            int index)
        {
            if (node is null || index >= segments.Count)
                return;

            var segment = segments[index];
            var navigation = sourceEntityType.FindNavigation(segment);
            if (navigation is null)
                return;

            var targetType = navigation.TargetEntityType;
            var isLast = index == segments.Count - 1;

            var rowDict = AsReadOnlyStringDictionary(node);
            if (rowDict is null)
                return;

            if (!TryGetProjectionNavigationValue(rowDict, segment, out var raw))
                return;

            if (navigation.IsCollection) {
                if (raw is IEnumerable enumerable && raw is not string && raw is not byte[]) {
                    foreach (var el in enumerable) {
                        var childDict = AsReadOnlyStringDictionary(el);
                        if (childDict is null)
                            continue;

                        TryAddDictEntityTag(childDict, targetType);
                        if (!isLast)
                            WalkProjectionInclude(childDict, targetType, segments, index + 1);
                    }
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
    {
        return node switch {
            IReadOnlyDictionary<string, object?> d => d,
            IDictionary<string, object?> mutable => new Dictionary<string, object?>(mutable, StringComparer.OrdinalIgnoreCase),
            _ => null,
        };
    }

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
