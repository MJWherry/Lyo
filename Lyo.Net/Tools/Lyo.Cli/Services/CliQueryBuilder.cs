using System.Text.Json;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using FromClause = Lyo.Query.Models.Common.Request.FromClause;

namespace Lyo.Cli.Services;

internal enum QueryMode
{
    Concrete,
    Project,
    Root
}

/// <summary>Builds Lyo query request DTOs from CLI flags.</summary>
internal static class CliQueryBuilder
{
    public static QueryMode ParseMode(string mode)
        => mode.Trim().ToLowerInvariant() switch {
            "concrete" => QueryMode.Concrete,
            "project" => QueryMode.Project,
            "root" => QueryMode.Root,
            var _ => throw new ArgumentException($"Unknown query mode '{mode}'. Use concrete, project, or root.")
        };

    public static object Build(
        QueryMode mode,
        IEnumerable<string>? wheres,
        string? whereFileJson,
        IEnumerable<string>? includes,
        IEnumerable<string>? sorts,
        int? start,
        int? amount,
        IEnumerable<string>? keys,
        IEnumerable<string>? selects,
        string? from)
    {
        var where = BuildWhere(wheres, whereFileJson);
        return mode switch {
            QueryMode.Concrete => BuildConcrete(where, includes, sorts, start, amount, keys),
            QueryMode.Project => BuildProject(where, includes, sorts, start, amount, keys, selects),
            QueryMode.Root => BuildRoot(where, includes, sorts, start, amount, keys, selects, from),
            var _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    public static string Serialize(object request)
    {
        var options = LyoJsonSerializerOptions.Create(o => o.WriteIndented = true);
        return JsonSerializer.Serialize(request, request.GetType(), options);
    }

    private static QueryConcreteReq BuildConcrete(
        WhereClause? where,
        IEnumerable<string>? includes,
        IEnumerable<string>? sorts,
        int? start,
        int? amount,
        IEnumerable<string>? keys)
    {
        var b = QueryConcreteReqBuilder.New();
        if (where is not null)
            b.AddWhere(where);
        foreach (var i in includes ?? [])
            b.AddIncludes(i);
        foreach (var s in sorts ?? []) {
            var (field, dir) = ParseSort(s);
            b.AddSort(field, dir);
        }

        if (start is not null || amount is not null)
            b.SetPagination(start ?? 0, amount ?? 20);
        foreach (var k in keys ?? [])
            b.AddKey(k);
        return b.Build();
    }

    private static ProjectionQueryReq BuildProject(
        WhereClause? where,
        IEnumerable<string>? includes,
        IEnumerable<string>? sorts,
        int? start,
        int? amount,
        IEnumerable<string>? keys,
        IEnumerable<string>? selects)
    {
        var b = ProjectionQueryReqBuilder.New();
        if (where is not null)
            b.AddWhere(where);
        foreach (var i in includes ?? [])
            b.AddIncludes(i);
        foreach (var s in sorts ?? []) {
            var (field, dir) = ParseSort(s);
            b.AddSort(field, dir);
        }

        if (start is not null || amount is not null)
            b.SetPagination(start ?? 0, amount ?? 20);
        foreach (var k in keys ?? [])
            b.AddKey(k);
        foreach (var sel in selects ?? [])
            b.AddSelects(sel);
        return b.Build();
    }

    private static QueryReq BuildRoot(
        WhereClause? where,
        IEnumerable<string>? includes,
        IEnumerable<string>? sorts,
        int? start,
        int? amount,
        IEnumerable<string>? keys,
        IEnumerable<string>? selects,
        string? from)
    {
        var b = QueryReqBuilder.New();
        if (where is not null)
            b.AddWhere(where);
        foreach (var s in sorts ?? []) {
            var (field, dir) = ParseSort(s);
            b.AddSort(field, dir);
        }

        if (start is not null || amount is not null)
            b.SetPagination(start ?? 0, amount ?? 20);
        foreach (var k in keys ?? [])
            b.AddKey(k);
        foreach (var sel in selects ?? [])
            b.AddSelects(sel);
        if (!string.IsNullOrWhiteSpace(from)) {
            var parts = from.Split(':', 2, StringSplitOptions.TrimEntries);
            ArgumentHelpers.ThrowIf(parts.Length != 2, "--from must be ALIAS:ENTITY");
            b.From(new FromClause { Alias = parts[0], EntityType = parts[1] });
        }

        _ = includes; // root queries use From/Joins; includes ignored
        return b.Build();
    }

    private static (string Field, SortDirection Dir) ParseSort(string sort)
    {
        var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries);
        var field = parts[0];
        var dir = SortDirection.Asc;
        if (parts.Length == 2) {
            dir = parts[1].ToLowerInvariant() switch {
                "asc" => SortDirection.Asc,
                "desc" => SortDirection.Desc,
                var _ => throw new ArgumentException($"Unknown sort direction '{parts[1]}' in '{sort}'.")
            };
        }

        return (field, dir);
    }

    private static WhereClause? BuildWhere(IEnumerable<string>? wheres, string? whereFileJson)
    {
        WhereClause? fromFile = null;
        if (!string.IsNullOrWhiteSpace(whereFileJson)) {
            fromFile = JsonSerializer.Deserialize<WhereClause>(whereFileJson, LyoJsonSerializerOptions.Create());
            ArgumentHelpers.ThrowIf(fromFile is null, "Failed to deserialize --where-file JSON.");
        }

        var list = wheres?.ToArray() ?? [];
        if (list.Length == 0)
            return fromFile;

        var b = WhereClauseBuilder.And();
        foreach (var w in list)
            ApplyWhereFlag(b, w);

        var built = b.Build();
        if (fromFile is null)
            return built;

        var merge = WhereClauseBuilder.And();
        merge.Add(fromFile);
        merge.Add(built);
        return merge.Build();
    }

    private static void ApplyWhereFlag(WhereClauseBuilder b, string flag)
    {
        var first = flag.IndexOf(':');
        ArgumentHelpers.ThrowIf(first <= 0, $"Invalid --where '{flag}'. Expected FIELD:OP:VALUE.");
        var second = flag.IndexOf(':', first + 1);
        ArgumentHelpers.ThrowIf(second <= first + 1, $"Invalid --where '{flag}'. Expected FIELD:OP:VALUE.");
        var field = flag[..first];
        var op = flag[(first + 1)..second];
        var value = flag[(second + 1)..];

        switch (op.ToLowerInvariant()) {
            case "eq" or "equals":
                b.Equals(field, Coerce(value));
                break;
            case "ne" or "neq" or "notequals":
                b.NotEquals(field, Coerce(value));
                break;
            case "gt":
                b.GreaterThan(field, Coerce(value)!);
                break;
            case "gte":
                b.GreaterThanOrEqual(field, Coerce(value)!);
                break;
            case "lt":
                b.LessThan(field, Coerce(value)!);
                break;
            case "lte":
                b.LessThanOrEqual(field, Coerce(value)!);
                break;
            case "contains":
                b.Contains(field, Coerce(value));
                break;
            case "starts" or "startswith":
                b.StartsWith(field, value);
                break;
            case "ends" or "endswith":
                b.EndsWith(field, value);
                break;
            case "in":
                b.In(field, value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(Coerce).ToArray<object?>());
                break;
            default:
                throw new ArgumentException($"Unknown where operator '{op}' in '{flag}'.");
        }
    }

    private static object? Coerce(string value)
    {
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;
        if (bool.TryParse(value, out var b))
            return b;
        if (long.TryParse(value, out var l))
            return l;
        if (decimal.TryParse(value, out var d))
            return d;
        return value;
    }
}
