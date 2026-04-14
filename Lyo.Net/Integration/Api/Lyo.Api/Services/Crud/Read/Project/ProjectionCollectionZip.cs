using System.Collections;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Api.Services.Crud.Read.Project;

/// <summary>
/// Zips parallel IEnumerable columns (one list per selected path) into nested objects under collection roots.
/// Model: every field path is <c>segment.segment.leaf</c>; merge keys are prefixes that end at an <see cref="ICollection{T}" />
/// on the entity graph. Planning runs once per query shape; application runs per row.
/// </summary>
internal static class ProjectionCollectionZip
{
    internal sealed record SiblingCollectionMergeGroup(string MergeKey, IReadOnlyList<ProjectedFieldSpec> Specs);

    /// <summary>
    /// Builds merge steps: unified root collection (mixed path depths), sibling merges under a common prefix, then augment from parallel columns (incl. computed).
    /// </summary>
    internal static IReadOnlyList<SiblingCollectionMergeGroup> PlanMergeGroups(
        Type entityType,
        IReadOnlyList<ProjectedFieldSpec> specs,
        IReadOnlyList<object?> items)
    {
        var unifiedGroups = FindUnifiedRootCollectionMergeGroups(entityType, specs);
        var unifiedRootKeys = unifiedGroups.Select(g => g.MergeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var findGroups = FindSiblingCollectionMergeGroups(entityType, specs, unifiedRootKeys);
        var merged = new List<SiblingCollectionMergeGroup>(unifiedGroups.Count + findGroups.Count);
        merged.AddRange(unifiedGroups);
        merged.AddRange(findGroups);
        return AugmentMergeGroupsForSingleSelectWithComputedParallelColumns(entityType, specs, items, merged, unifiedRootKeys);
    }

    /// <summary>
    /// SQL slot planning matches row zip (unified root + sibling groups) but never augments from a sample row.
    /// Unified-root groups must be included here; otherwise every path under the same collection root is excluded from
    /// <see cref="FindSiblingCollectionMergeGroups" /> and EF gets one slot per field (multiple joins / wider shapes).
    /// </summary>
    internal static IReadOnlyList<SiblingCollectionMergeGroup> PlanMergeGroupsForSqlSlots(Type entityType, IReadOnlyList<ProjectedFieldSpec> specs)
    {
        var unifiedGroups = FindUnifiedRootCollectionMergeGroups(entityType, specs);
        var unifiedRootKeys = unifiedGroups.Select(g => g.MergeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var findGroups = FindSiblingCollectionMergeGroups(entityType, specs, unifiedRootKeys);
        if (unifiedGroups.Count == 0)
            return findGroups;

        var merged = new List<SiblingCollectionMergeGroup>(unifiedGroups.Count + findGroups.Count);
        merged.AddRange(unifiedGroups);
        merged.AddRange(findGroups);
        return merged;
    }

    internal static void ApplyMergeGroupsToRows(
        IReadOnlyList<object?> items,
        IReadOnlyList<SiblingCollectionMergeGroup> orderedGroups,
        IReadOnlyList<ProjectedFieldSpec> allSpecs)
    {
        foreach (var item in items) {
            if (item is Dictionary<string, object?> dict)
                MergeSiblingCollectionProjectionIntoDictionary(dict, orderedGroups, allSpecs);
        }
    }

    private static void MergeSiblingCollectionProjectionIntoDictionary(
        Dictionary<string, object?> row,
        IReadOnlyList<SiblingCollectionMergeGroup> groups,
        IReadOnlyList<ProjectedFieldSpec> allSpecs)
    {
        foreach (var group in groups) {
            var mergeKey = group.MergeKey;
            if (MergeKeyIsUnderParentScopeWildcard(allSpecs, mergeKey))
                continue;

            var specPaths = new HashSet<string>(group.Specs.Select(s => s.RequestedPath), StringComparer.OrdinalIgnoreCase);
            var scopeWildcardSpec = group.Specs.FirstOrDefault(s => string.Equals(s.NormalizedParts[^1], "*", StringComparison.Ordinal));
            var collectionPrefix = GetCollectionRequestedPathPrefix(group.Specs[0].RequestedPath);

            var extraZipColumns = new List<(string RowKey, string RelativePath)>();
            foreach (var kvp in row) {
                if (specPaths.Contains(kvp.Key))
                    continue;
                if (kvp.Value is string or byte[] or not IEnumerable)
                    continue;

                if (scopeWildcardSpec != null) {
                    if (!kvp.Key.StartsWith(mergeKey + ".", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.Equals(kvp.Key, scopeWildcardSpec.RequestedPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var rel = kvp.Key[(mergeKey.Length + 1)..];
                    extraZipColumns.Add((kvp.Key, rel));
                    continue;
                }

                if (IsUnifiedRootCollectionMergeGroup(group)) {
                    if (!kvp.Key.StartsWith(mergeKey + ".", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var relU = kvp.Key[(mergeKey.Length + 1)..];
                    extraZipColumns.Add((kvp.Key, relU));
                    continue;
                }

                var pfx = GetCollectionRequestedPathPrefix(kvp.Key);
                if (!string.Equals(pfx, collectionPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var leafStart = pfx.Length + 1;
                if (leafStart >= kvp.Key.Length)
                    continue;

                extraZipColumns.Add((kvp.Key, kvp.Key[leafStart..]));
            }

            var maxLen = 0;
            foreach (var spec in group.Specs) {
                if (row.TryGetValue(spec.RequestedPath, out var colVal) && colVal != null)
                    maxLen = Math.Max(maxLen, GetEnumerableLength(colVal));
            }

            foreach (var (rowKey, _) in extraZipColumns) {
                if (row.TryGetValue(rowKey, out var colVal) && colVal != null)
                    maxLen = Math.Max(maxLen, GetEnumerableLength(colVal));
            }

            var merged = new List<Dictionary<string, object?>>(maxLen);
            for (var i = 0; i < maxLen; i++) {
                var capacity = group.Specs.Count + extraZipColumns.Count;
                var obj = new Dictionary<string, object?>(capacity, StringComparer.OrdinalIgnoreCase);
                foreach (var spec in group.Specs) {
                    row.TryGetValue(spec.RequestedPath, out var colVal);
                    var leafName = spec.NormalizedParts[^1];
                    if (string.Equals(leafName, "*", StringComparison.Ordinal)) {
                        var el = GetElementAtIndex(colVal, i);
                        if (el is Dictionary<string, object?> scopeDict) {
                            foreach (var kv in scopeDict)
                                obj[kv.Key] = kv.Value;
                        }
                        else if (el != null)
                            obj["*"] = el;
                    }
                    else if (IsUnifiedRootCollectionMergeGroup(group)) {
                        var req = spec.RequestedPath;
                        var tail = req.Length > mergeKey.Length + 1
                            && req.StartsWith(mergeKey, StringComparison.OrdinalIgnoreCase)
                            && req[mergeKey.Length] == '.'
                            ? req[(mergeKey.Length + 1)..]
                            : leafName;
                        var cell = GetElementAtIndex(colVal, i);
                        if (tail.Contains('.', StringComparison.Ordinal))
                            AssignNestedProperty(obj, tail, cell);
                        else
                            UpsertCaseInsensitive(obj, tail, cell);
                    }
                    else
                        obj[leafName] = GetElementAtIndex(colVal, i);
                }

                foreach (var (rowKey, relativePath) in extraZipColumns) {
                    row.TryGetValue(rowKey, out var colVal);
                    var cell = GetElementAtIndex(colVal, i);
                    if (relativePath.Contains('.', StringComparison.Ordinal))
                        AssignNestedProperty(obj, relativePath, cell);
                    else
                        obj[relativePath] = cell;
                }

                merged.Add(obj);
            }

            foreach (var spec in group.Specs)
                row.Remove(spec.RequestedPath);

            foreach (var (rowKey, _) in extraZipColumns)
                row.Remove(rowKey);

            row[group.MergeKey] = merged;
        }
    }

    private static void AssignNestedProperty(Dictionary<string, object?> root, string dottedPath, object? value)
    {
        var segments = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return;

        var cur = root;
        for (var i = 0; i < segments.Length - 1; i++) {
            if (!TryGetOrCreateNestedDict(cur, segments[i], out var next))
                return;

            cur = next;
        }

        UpsertCaseInsensitive(cur, segments[^1], value);
    }

    private static bool TryGetOrCreateNestedDict(Dictionary<string, object?> parent, string segment, out Dictionary<string, object?> child)
    {
        foreach (var kv in parent) {
            if (!string.Equals(kv.Key, segment, StringComparison.OrdinalIgnoreCase))
                continue;
            if (kv.Value is Dictionary<string, object?> d) {
                child = d;
                return true;
            }

            child = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            parent[kv.Key] = child;
            return true;
        }

        child = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        parent[segment] = child;
        return true;
    }

    private static void UpsertCaseInsensitive(Dictionary<string, object?> dict, string key, object? value)
    {
        foreach (var k in dict.Keys) {
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) {
                dict[k] = value;
                return;
            }
        }

        dict[key] = value;
    }

    private static IReadOnlyList<SiblingCollectionMergeGroup> FindUnifiedRootCollectionMergeGroups(Type rootType, IReadOnlyList<ProjectedFieldSpec> specs)
    {
        var byFirst = new Dictionary<string, List<ProjectedFieldSpec>>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in specs) {
            if (spec.NormalizedParts.Length < 2)
                continue;
            if (NormalizedPartsContainWildcard(spec.NormalizedParts))
                continue;

            var first = spec.NormalizedParts[0];
            if (!TryRootPropertyIsCollection(rootType, first))
                continue;

            if (!byFirst.TryGetValue(first, out var bucket)) {
                bucket = [];
                byFirst[first] = bucket;
            }

            bucket.Add(spec);
        }

        var result = new List<SiblingCollectionMergeGroup>();
        foreach (var (first, bucket) in byFirst) {
            if (bucket.Count < 2)
                continue;

            if (bucket.Any(s => NormalizedPartsContainWildcard(s.NormalizedParts)))
                continue;

            var distinctParentPaths = bucket
                .Select(s => string.Join(".", s.NormalizedParts[..^1]))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (distinctParentPaths.Count <= 1)
                continue;

            result.Add(new(first, bucket));
        }

        return result;
    }

    private static bool TryRootPropertyIsCollection(Type rootType, string firstSegment)
    {
        if (SharedEntityMetadataCache.IsCollectionType(rootType))
            return false;

        var property = SharedEntityMetadataCache.ResolveProperty(rootType, firstSegment);
        if (property is null)
            return false;

        return SharedEntityMetadataCache.IsCollectionType(property.PropertyType);
    }

    private static IReadOnlyList<SiblingCollectionMergeGroup> FindSiblingCollectionMergeGroups(
        Type rootType,
        IReadOnlyList<ProjectedFieldSpec> specs,
        HashSet<string>? excludeFirstSegmentsFromUnifiedMerge = null)
    {
        var multiSegmentEligible = 0;
        foreach (var spec in specs) {
            if (spec.NormalizedParts.Length >= 2 && !NormalizedPartsContainWildcard(spec.NormalizedParts))
                multiSegmentEligible++;
        }

        if (multiSegmentEligible < 2)
            return [];

        var byPrefix = new Dictionary<string, List<ProjectedFieldSpec>>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in specs) {
            if (spec.NormalizedParts.Length < 2)
                continue;

            if (NormalizedPartsContainWildcard(spec.NormalizedParts))
                continue;

            if (excludeFirstSegmentsFromUnifiedMerge != null
                && excludeFirstSegmentsFromUnifiedMerge.Contains(spec.NormalizedParts[0]))
                continue;

            var prefixKey = string.Join(".", spec.NormalizedParts, 0, spec.NormalizedParts.Length - 1);
            if (!byPrefix.TryGetValue(prefixKey, out var bucket)) {
                bucket = [];
                byPrefix[prefixKey] = bucket;
            }

            bucket.Add(spec);
        }

        var result = new List<SiblingCollectionMergeGroup>();
        foreach (var bucket in byPrefix.Values) {
            if (bucket.Count < 2)
                continue;

            var leaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicateLeaf = false;
            foreach (var spec in bucket) {
                if (!leaves.Add(spec.NormalizedParts[^1])) {
                    duplicateLeaf = true;
                    break;
                }
            }

            if (duplicateLeaf)
                continue;

            var prefixParts = bucket[0].NormalizedParts[..^1].ToArray();
            if (!TryPathPrefixAllowsNestedSiblingZip(rootType, prefixParts))
                continue;

            var mergeKey = string.Join(".", prefixParts);
            if (MergeKeyIsUnderParentScopeWildcard(specs, mergeKey))
                continue;

            result.Add(new(mergeKey, bucket));
        }

        return result;
    }

    private static bool MergeKeyIsUnderParentScopeWildcard(IReadOnlyList<ProjectedFieldSpec> specs, string mergeKey)
    {
        foreach (var spec in specs) {
            if (spec.NormalizedParts.Length < 2)
                continue;
            if (!string.Equals(spec.NormalizedParts[^1], "*", StringComparison.Ordinal))
                continue;

            var scope = string.Join(".", spec.NormalizedParts[..^1]);
            if (scope.Length == 0)
                continue;
            if (mergeKey.Length > scope.Length && mergeKey.StartsWith(scope + ".", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool MergeKeyIsStrictlyUnderUnifiedRootCollection(HashSet<string> unifiedRoots, string mergeKey)
    {
        foreach (var u in unifiedRoots) {
            if (mergeKey.Length > u.Length && mergeKey.StartsWith(u + ".", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsUnifiedRootCollectionMergeGroup(SiblingCollectionMergeGroup g)
    {
        if (g.Specs.Count < 2)
            return false;

        var firstSeg = g.Specs[0].NormalizedParts[0];
        if (!string.Equals(g.MergeKey, firstSeg, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!g.Specs.All(s =>
                s.NormalizedParts.Length >= 2 && string.Equals(s.NormalizedParts[0], firstSeg, StringComparison.OrdinalIgnoreCase)))
            return false;

        var parentPaths = g.Specs.Select(s => string.Join(".", s.NormalizedParts[..^1])).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return parentPaths.Count > 1;
    }

    private static bool TryPathPrefixAllowsNestedSiblingZip(Type rootType, string[] prefixParts)
    {
        if (prefixParts.Length == 0)
            return false;

        if (TryPathEndsAtCollection(rootType, prefixParts))
            return true;

        var crossedCollectionProperty = false;
        var current = rootType;
        foreach (var part in prefixParts) {
            if (SharedEntityMetadataCache.IsCollectionType(current))
                current = SharedEntityMetadataCache.GetCollectionElementType(current);

            var property = SharedEntityMetadataCache.ResolveProperty(current, part);
            if (property is null)
                return false;

            if (SharedEntityMetadataCache.IsCollectionType(property.PropertyType))
                crossedCollectionProperty = true;

            current = property.PropertyType;
        }

        return crossedCollectionProperty;
    }

    private static IReadOnlyList<SiblingCollectionMergeGroup> AugmentMergeGroupsForSingleSelectWithComputedParallelColumns(
        Type entityType,
        IReadOnlyList<ProjectedFieldSpec> specs,
        IReadOnlyList<object?> items,
        IReadOnlyList<SiblingCollectionMergeGroup> existingGroups,
        HashSet<string>? unifiedRootCollectionKeys = null)
    {
        var existingKeys = new HashSet<string>(existingGroups.Select(g => g.MergeKey), StringComparer.OrdinalIgnoreCase);
        var added = new List<SiblingCollectionMergeGroup>();

        Dictionary<string, object?>? firstDict = null;
        foreach (var item in items) {
            if (item is Dictionary<string, object?> d) {
                firstDict = d;
                break;
            }
        }

        if (firstDict is null)
            return existingGroups;

        foreach (var spec in specs) {
            if (spec.NormalizedParts.Length < 2)
                continue;

            var scopeTrailingStar = string.Equals(spec.NormalizedParts[^1], "*", StringComparison.Ordinal);
            if (!scopeTrailingStar && NormalizedPartsContainWildcard(spec.NormalizedParts))
                continue;

            var mergeKey = string.Join(".", spec.NormalizedParts[..^1]);
            if (MergeKeyIsUnderParentScopeWildcard(specs, mergeKey))
                continue;

            if (unifiedRootCollectionKeys is { Count: > 0 } && MergeKeyIsStrictlyUnderUnifiedRootCollection(unifiedRootCollectionKeys, mergeKey))
                continue;

            if (existingKeys.Contains(mergeKey))
                continue;

            var parallelSiblingCount = 0;
            if (scopeTrailingStar) {
                foreach (var kvp in firstDict) {
                    if (kvp.Value is string or byte[] || kvp.Value is not IEnumerable)
                        continue;

                    if (!kvp.Key.StartsWith(mergeKey + ".", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.Equals(kvp.Key, spec.RequestedPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    parallelSiblingCount++;
                }

                if (parallelSiblingCount < 1)
                    continue;
            }
            else {
                var collectionPrefix = GetCollectionRequestedPathPrefix(spec.RequestedPath);
                foreach (var kvp in firstDict) {
                    if (kvp.Value is string or byte[] || kvp.Value is not IEnumerable)
                        continue;

                    if (!string.Equals(GetCollectionRequestedPathPrefix(kvp.Key), collectionPrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    parallelSiblingCount++;
                }

                if (parallelSiblingCount < 2)
                    continue;
            }

            var prefixParts = spec.NormalizedParts[..^1].ToArray();
            if (!TryPathPrefixAllowsNestedSiblingZip(entityType, prefixParts))
                continue;

            added.Add(new(mergeKey, [spec]));
            existingKeys.Add(mergeKey);
        }

        if (added.Count == 0)
            return existingGroups;

        var combined = new List<SiblingCollectionMergeGroup>(existingGroups.Count + added.Count);
        combined.AddRange(existingGroups);
        combined.AddRange(added);
        return combined;
    }

    internal static bool NormalizedPartsContainWildcard(string[] parts)
    {
        for (var i = 0; i < parts.Length; i++) {
            if (parts[i] == "*")
                return true;
        }

        return false;
    }

    private static bool TryPathEndsAtCollection(Type rootType, string[] prefixParts)
    {
        if (prefixParts.Length == 0)
            return false;

        var current = rootType;
        foreach (var part in prefixParts) {
            if (SharedEntityMetadataCache.IsCollectionType(current))
                current = SharedEntityMetadataCache.GetCollectionElementType(current);

            var property = SharedEntityMetadataCache.ResolveProperty(current, part);
            if (property == null)
                return false;

            current = property.PropertyType;
        }

        return SharedEntityMetadataCache.IsCollectionType(current);
    }

    private static int GetEnumerableLength(object? value)
    {
        if (value is null)
            return 0;

        if (value is ICollection collection)
            return collection.Count;

        if (value is IEnumerable enumerable and not string and not byte[]) {
            var n = 0;
            foreach (var _ in enumerable)
                n++;

            return n;
        }

        return 0;
    }

    private static object? GetElementAtIndex(object? value, int index)
    {
        if (value is null)
            return null;

        if (value is IList list)
            return index < list.Count ? list[index] : null;

        if (value is IEnumerable enumerable and not string and not byte[]) {
            var i = 0;
            foreach (var x in enumerable) {
                if (i == index)
                    return x;

                i++;
            }
        }

        return null;
    }

    private static string GetCollectionRequestedPathPrefix(string fullPath)
    {
        var i = fullPath.LastIndexOf('.');
        return i <= 0 ? fullPath : fullPath[..i];
    }
}
