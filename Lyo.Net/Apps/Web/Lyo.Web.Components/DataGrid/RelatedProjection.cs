using System.Linq.Expressions;
using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common.Core;
using Lyo.Common.Json;
using Lyo.Exceptions;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Select inference, FK extraction, key-row shaping, and deserialize helpers for related-entity QueryProject lookups.
/// </summary>
public static class RelatedProjection
{
    /// <summary>Dynamic CRUD QueryProject <c>MaxKeySetCount</c>; typed endpoints allow more, so this is the safe chunk size.</summary>
    public const int DefaultKeyChunkSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();

    /// <summary>
    /// Select paths for a related QueryProject. Explicit <paramref name="select" /> wins; otherwise public scalar properties of <paramref name="resType" />.
    /// Always unions <c>Id</c> so the lookup can be keyed.
    /// </summary>
    public static IReadOnlyList<string> InferSelect(Type resType, IReadOnlyList<string>? select = null)
    {
        ArgumentHelpers.ThrowIfNull(resType);
        IEnumerable<string> names;
        if (select is { Count: > 0 })
            names = select.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim());
        else {
            names = resType.ScalarProperties().Select(p => p.Name);
        }

        return names.Append("Id").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>True for primitives, enums, string, Guid, dates, decimal, and Uri (including nullable wrappers).</summary>
    public static bool IsScalar(Type type) => type.IsScalar();

    /// <summary>Reads a field from a projected row (JSON/dictionary) or a typed POCO via reflection.</summary>
    public static object? GetFieldValue(object? item, string fieldName) => ProjectedValueHelper.GetValue(item, fieldName);

    /// <summary>Stable non-empty id string for cache keys. Empty Guid and blank values return null.</summary>
    public static string? IdKey(object? value)
    {
        if (value is null)
            return null;

        if (value is JsonElement je) {
            if (je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return null;
            if (je.ValueKind == JsonValueKind.String)
                return IdKey(je.GetString());
            if (je.TryGetGuid(out var guidFromJson))
                return IdKey(guidFromJson);
            return IdKey(je.ToString());
        }

        if (value is Guid guid)
            return guid == Guid.Empty ? null : guid.ToString();

        if (ProjectedValueHelper.TryGetGuid(value, out var parsed) && parsed != Guid.Empty)
            return parsed.ToString();

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>Distinct FK values from <paramref name="rows" /> across <paramref name="fields" />, dropping empties. Guids stay Guids for <c>Keys</c>.</summary>
    public static IReadOnlyList<object> CollectIds(IEnumerable<object?> rows, IEnumerable<string> fields)
    {
        ArgumentHelpers.ThrowIfNull(rows);
        ArgumentHelpers.ThrowIfNull(fields);
        var fieldList = fields.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new List<object>();
        foreach (var row in rows) {
            foreach (var field in fieldList) {
                var raw = GetFieldValue(row, field);
                var key = IdKey(raw);
                if (key is null || !seen.Add(key))
                    continue;

                ids.Add(ProjectedValueHelper.TryGetGuid(raw, out var guid) ? guid : raw!);
            }
        }

        return ids;
    }

    /// <summary>Turns ids into QueryProject <c>Keys</c> rows <c>[[id], …]</c>, chunked to <paramref name="chunkSize" />.</summary>
    public static IReadOnlyList<IReadOnlyList<object[]>> ChunkKeys(IEnumerable<object> ids, int chunkSize = DefaultKeyChunkSize)
    {
        ArgumentHelpers.ThrowIfNull(ids);
        ArgumentHelpers.ThrowIf(chunkSize < 1, "Chunk size must be at least 1.", nameof(chunkSize));
        var rows = ids.Select(id => new object[] { id }).ToList();
        if (rows.Count == 0)
            return [];

        var chunks = new List<IReadOnlyList<object[]>>();
        for (var i = 0; i < rows.Count; i += chunkSize)
            chunks.Add(rows.GetRange(i, Math.Min(chunkSize, rows.Count - i)));

        return chunks;
    }

    /// <summary>Groups visible related columns by route and result type, unioning Select paths and parent FK fields.</summary>
    public static IReadOnlyList<RelatedLoadGroup> GroupVisible(RelatedColumnRegistry registry)
    {
        ArgumentHelpers.ThrowIfNull(registry);
        return registry.GetVisible()
            .GroupBy(c => (c.Route, c.ResType))
            .Select(g => new RelatedLoadGroup {
                Route = g.Key.Route,
                ResType = g.Key.ResType,
                Select = g.SelectMany(c => c.Select).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Fields = g.Select(c => c.Field).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                ApiClient = g.Select(c => c.ApiClient).FirstOrDefault(c => c is not null)
            })
            .ToList();
    }

    /// <summary>Maps a projected QueryProject item onto <paramref name="resType" />.</summary>
    public static object? Deserialize(object? item, Type resType)
    {
        ArgumentHelpers.ThrowIfNull(resType);
        if (item is null)
            return null;
        if (resType.IsInstanceOfType(item))
            return item;
        if (item is JsonElement je)
            return je.Deserialize(resType, JsonOptions);
        if (item is JsonDocument doc)
            return doc.RootElement.Deserialize(resType, JsonOptions);

        var json = JsonSerializer.Serialize(item, JsonOptions);
        return JsonSerializer.Deserialize(json, resType, JsonOptions);
    }

    /// <inheritdoc cref="Deserialize(object?, Type)" />
    public static TRes? Deserialize<TRes>(object? item) => (TRes?)Deserialize(item, typeof(TRes));

    /// <summary>Dotted property path from a lambda, unwrapping convert nodes (same rules as <c>LyoPropertyColumn</c>).</summary>
    public static string? PropertyPath(LambdaExpression? expr) => expr.TryGetMemberPath();
}

/// <summary>One batched related QueryProject: shared route, result type, Select, and parent FK fields.</summary>
public sealed class RelatedLoadGroup
{
    /// <summary>Related QueryProject base route.</summary>
    public required string Route { get; init; }

    /// <summary>CLR type used to deserialize related rows.</summary>
    public required Type ResType { get; init; }

    /// <summary>Union of Select paths from grouped columns.</summary>
    public required IReadOnlyList<string> Select { get; init; }

    /// <summary>Parent FK fields whose values are collected into <c>Keys</c>.</summary>
    public required IReadOnlyList<string> Fields { get; init; }

    /// <summary>Optional client override from a column in the group.</summary>
    public IApiClient? ApiClient { get; init; }
}
