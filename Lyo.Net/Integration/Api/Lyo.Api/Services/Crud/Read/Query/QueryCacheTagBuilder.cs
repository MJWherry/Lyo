using System.Collections;
using Lyo.Api.Services.TypeConversion;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Builds cache tags for basic (non-projection) query results. Keys remain in <see cref="QueryCacheKeyBuilder" />.</summary>
public static class QueryCacheTagBuilder
{
    /// <summary>Tag applied to all cached list-query entries.</summary>
    /// <summary>Broad tag for all list-query cache entries (same as SQL projection path and historical convention).</summary>
    public const string QueryScopeTag = "queries";

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
}
