using System.Text;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Sql;

/// <summary>
/// Prints a concrete, projection, or root query request as one parameterized Postgres statement. Does not open a connection and does not build an
/// <c>IQueryable</c>.
/// </summary>
public sealed class SqlQueryTranslator
{
    /// <summary>Prints <paramref name="request" /> as <c>SELECT</c> of every mapped column.</summary>
    /// <param name="request">Concrete query. <c>Include</c> and <c>Keys</c> are rejected.</param>
    /// <param name="map">Table and scalar columns for the route entity.</param>
    /// <returns>Command text and parameters. Not executed.</returns>
    public SqlQuery Translate(QueryConcreteReq request, SqlColumnMap map)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(map);
        RejectShared(request);
        var parameters = new ParameterList();
        var where = RenderWhere(request.WhereClause, field => Column(map, field), parameters);
        var sql = new StringBuilder();
        sql.Append("SELECT ");
        sql.Append(string.Join(", ", map.Columns.Values.Select(Quote)));
        sql.Append(" FROM ");
        sql.Append(Table(map));
        AppendWhere(sql, where);
        AppendOrder(sql, request.SortBy, field => Column(map, field));
        AppendPage(sql, request.Start, request.Amount);
        return Finish(sql, parameters);
    }

    /// <summary>Prints <paramref name="request" /> as a scalar projection, or as <c>GROUP BY</c> when <see cref="ProjectionQueryReq.Aggregates" /> is set.</summary>
    /// <param name="request">Projection query. Computed fields, includes, and keys are rejected.</param>
    /// <param name="map">Table and scalar columns for the route entity.</param>
    /// <returns>Command text and parameters. Not executed.</returns>
    public SqlQuery Translate(ProjectionQueryReq request, SqlColumnMap map)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(map);
        RejectShared(request);
        RejectComputed(request.ComputedFields);
        if (request.Select.Count == 0 && request.Aggregates.Count == 0)
            throw new SqlTranslationException("Projection query needs a select or an aggregate.");

        var parameters = new ParameterList();
        var where = RenderWhere(request.WhereClause, field => Column(map, field), parameters);
        var select = new List<string>(request.Select.Count + request.Aggregates.Count);
        var group = new List<string>(request.Select.Count);
        foreach (var field in request.Select) {
            var column = Column(map, field);
            select.Add(column);
            group.Add(column);
        }

        var aggregateNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var aggregate in request.Aggregates) {
            if (string.IsNullOrWhiteSpace(aggregate.Name))
                throw new SqlTranslationException("Aggregate name is required.");

            select.Add($"{Aggregate(aggregate, map)} AS {Quote(aggregate.Name)}");
            aggregateNames[aggregate.Name] = Quote(aggregate.Name);
        }

        var sql = new StringBuilder();
        sql.Append("SELECT ");
        sql.Append(string.Join(", ", select));
        sql.Append(" FROM ");
        sql.Append(Table(map));
        AppendWhere(sql, where);
        if (request.Aggregates.Count > 0 && group.Count > 0) {
            sql.Append(" GROUP BY ");
            sql.Append(string.Join(", ", group));
        }

        AppendOrder(sql, request.SortBy, field => aggregateNames.TryGetValue(field, out var alias) ? alias : Column(map, field));
        AppendPage(sql, request.Start, request.Amount);
        return Finish(sql, parameters);
    }

    /// <summary>Prints <paramref name="request" /> as <c>FROM</c> plus joins. Paths are <c>alias.property</c>.</summary>
    /// <param name="request">Root query. Computed fields, includes, and keys are rejected.</param>
    /// <param name="entities">Entity type name to table map. Lookup uses the dictionary comparer.</param>
    /// <returns>Command text and parameters. Not executed.</returns>
    public SqlQuery Translate(QueryReq request, IReadOnlyDictionary<string, SqlColumnMap> entities)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(entities);
        RejectShared(request);
        RejectComputed(request.ComputedFields);
        if (request.Select.Count == 0)
            throw new SqlTranslationException("Root query needs a select.");

        var aliases = BindAliases(request, entities);
        var parameters = new ParameterList();
        var filters = new List<string>();
        AppendScope(filters, request.From.Alias, request.From.Query, aliases, parameters);
        foreach (var join in request.Joins)
            AppendScope(filters, join.Alias, join.Query, aliases, parameters);

        var outer = RenderWhere(request.WhereClause, field => RootColumn(field, aliases), parameters);
        if (!string.IsNullOrEmpty(outer))
            filters.Add(outer);

        var sql = new StringBuilder();
        sql.Append("SELECT ");
        sql.Append(string.Join(", ", request.Select.Select(path => RootColumn(path, aliases))));
        sql.Append(" FROM ");
        sql.Append(AliasedTable(aliases[request.From.Alias]));
        foreach (var join in request.Joins) {
            sql.Append(' ');
            sql.Append(JoinKeyword(join.Type));
            sql.Append(' ');
            sql.Append(AliasedTable(aliases[join.Alias]));
            if (join.On.Count == 0)
                throw new SqlTranslationException($"Join '{join.Alias}' needs an ON clause.");

            sql.Append(" ON ");
            sql.Append(string.Join(" AND ", join.On.Select(on => $"{RootColumn(on.From, aliases)} = {RootColumn(on.To, aliases)}")));
        }

        AppendWhere(sql, Combine(filters));
        AppendOrder(sql, request.SortBy, field => RootColumn(field, aliases));
        AppendPage(sql, request.Start, request.Amount);
        return Finish(sql, parameters);
    }

    private static Dictionary<string, SqlAlias> BindAliases(QueryReq request, IReadOnlyDictionary<string, SqlColumnMap> entities)
    {
        var aliases = new Dictionary<string, SqlAlias>(StringComparer.Ordinal);
        AddAlias(aliases, request.From.Alias, request.From.EntityType, entities);
        foreach (var join in request.Joins)
            AddAlias(aliases, join.Alias, join.EntityType, entities);

        return aliases;
    }

    private static void AddAlias(Dictionary<string, SqlAlias> aliases, string alias, string entityType, IReadOnlyDictionary<string, SqlColumnMap> entities)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new SqlTranslationException("Query alias is required.");

        if (aliases.ContainsKey(alias))
            throw new SqlTranslationException($"Alias '{alias}' is already used.");

        if (string.IsNullOrWhiteSpace(entityType) || !entities.TryGetValue(entityType, out var map))
            throw new SqlTranslationException($"Unknown entity '{entityType}'.");

        aliases[alias] = new(alias, map);
    }

    private static void AppendScope(List<string> filters, string alias, SourceQueryScope? scope, Dictionary<string, SqlAlias> aliases, ParameterList parameters)
    {
        if (scope is null)
            return;

        if (scope.Keys.Count > 0)
            throw new SqlTranslationException("Keys are not translated to SQL.");

        var predicate = RenderWhere(scope.WhereClause, field => field.Contains('.') ? RootColumn(field, aliases) : Qualify(aliases[alias], field), parameters);
        if (!string.IsNullOrEmpty(predicate))
            filters.Add(predicate);
    }

    private static string Aggregate(AggregateSelect aggregate, SqlColumnMap map)
    {
        var hasField = !string.IsNullOrWhiteSpace(aggregate.Field);
        if (aggregate.Function == AggregateFunction.Count && !hasField)
            return "COUNT(*)";

        if (!hasField)
            throw new SqlTranslationException($"Aggregate '{aggregate.Name}' needs a field.");

        var column = Column(map, aggregate.Field!);
        return aggregate.Function switch {
            AggregateFunction.Count => $"COUNT({column})",
            AggregateFunction.CountDistinct => $"COUNT(DISTINCT {column})",
            AggregateFunction.Sum => $"SUM({column})",
            AggregateFunction.Average => $"AVG({column})",
            AggregateFunction.Min => $"MIN({column})",
            AggregateFunction.Max => $"MAX({column})",
            _ => throw new SqlTranslationException($"Aggregate '{aggregate.Function}' is not translated to SQL.")
        };
    }

    private static string? RenderWhere(WhereClause? clause, Func<string, string> column, ParameterList parameters)
    {
        if (clause is null)
            return null;

        RejectSubClause(clause);
        return clause switch {
            ConditionClause condition => Condition(condition, column, parameters),
            GroupClause group => Group(group, column, parameters),
            _ => throw new SqlTranslationException($"Where clause '{clause.GetType().Name}' is not translated to SQL.")
        };
    }

    private static void RejectSubClause(WhereClause clause)
    {
        if (clause.SubClause is not null)
            throw new SqlTranslationException("SubClause is not translated to SQL.");

        if (clause is GroupClause group) {
            foreach (var child in group.Children)
                RejectSubClause(child);
        }
    }

    private static string Group(GroupClause group, Func<string, string> column, ParameterList parameters)
    {
        if (group.Children.Count == 0)
            throw new SqlTranslationException("Group has no conditions.");

        var parts = group.Children.Select(child => RenderWhere(child, column, parameters) ?? throw new SqlTranslationException("Group child is empty.")).ToArray();
        if (parts.Length == 1)
            return parts[0];

        var op = group.Operator switch {
            GroupOperatorEnum.And => "AND",
            GroupOperatorEnum.Or => "OR",
            _ => throw new SqlTranslationException($"Group operator '{group.Operator}' is not translated to SQL.")
        };
        return string.Join($" {op} ", parts.Select(part => $"({part})"));
    }

    private static string Condition(ConditionClause condition, Func<string, string> column, ParameterList parameters)
    {
        if (condition.Comparison is ComparisonOperatorEnum.Regex or ComparisonOperatorEnum.NotRegex)
            throw new SqlTranslationException($"Comparison '{condition.Comparison}' is not translated to SQL.");

        var target = column(condition.Field);
        if (condition.Comparison is ComparisonOperatorEnum.In or ComparisonOperatorEnum.NotIn)
            return InList(condition, target, parameters);

        if (IsNull(condition.Value)) {
            return condition.Comparison switch {
                ComparisonOperatorEnum.Equals => $"{target} IS NULL",
                ComparisonOperatorEnum.NotEquals => $"{target} IS NOT NULL",
                _ => throw new SqlTranslationException($"Comparison '{condition.Comparison}' cannot compare null.")
            };
        }

        if (condition.Comparison is ComparisonOperatorEnum.Fuzzy or ComparisonOperatorEnum.NotFuzzy) {
            var probe = parameters.Add(Unwrap(condition.Value));
            var threshold = parameters.Add(condition.Threshold ?? ConditionClause.DefaultFuzzyThreshold);
            var similarity = $"similarity(lower({target}), lower({probe})) >= {threshold}";
            return condition.Comparison == ComparisonOperatorEnum.Fuzzy ? similarity : $"NOT ({similarity})";
        }

        var value = Unwrap(condition.Value);
        var parameter = parameters.Add(value);
        var folded = value is string;
        var left = folded ? $"lower({target})" : target;
        var right = folded ? $"lower({parameter})" : parameter;
        if (IsLike(condition.Comparison)) {
            var pattern = parameters.ReplaceLast(LikePattern(value as string ?? "", condition.Comparison));
            right = folded ? $"lower({pattern})" : pattern;
            var like = $"{left} LIKE {right} ESCAPE '\\'";
            return IsNegatedLike(condition.Comparison) ? $"NOT ({like})" : like;
        }

        var op = condition.Comparison switch {
            ComparisonOperatorEnum.Equals => "=",
            ComparisonOperatorEnum.NotEquals => "<>",
            ComparisonOperatorEnum.GreaterThan => ">",
            ComparisonOperatorEnum.GreaterThanOrEqual => ">=",
            ComparisonOperatorEnum.LessThan => "<",
            ComparisonOperatorEnum.LessThanOrEqual => "<=",
            _ => throw new SqlTranslationException($"Comparison '{condition.Comparison}' is not translated to SQL.")
        };
        return $"{left} {op} {right}";
    }

    private static string InList(ConditionClause condition, string target, ParameterList parameters)
    {
        var values = ReadList(condition.Value);
        var folded = values.All(value => value is string);
        var placeholders = values.Select(value => {
            var parameter = parameters.Add(value);
            return folded ? $"lower({parameter})" : parameter;
        });
        var left = folded ? $"lower({target})" : target;
        var list = $"{left} IN ({string.Join(", ", placeholders)})";
        return condition.Comparison == ComparisonOperatorEnum.NotIn ? $"NOT ({list})" : list;
    }

    private static List<object?> ReadList(object? value)
    {
        value = Unwrap(value);
        var list = new List<object?>();
        switch (value) {
            case null:
                break;
            case string csv:
                foreach (var part in csv.Split(',')) {
                    var trimmed = part.Trim();
                    if (trimmed.Length > 0)
                        list.Add(trimmed);
                }

                break;
            case System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Array } array:
                foreach (var item in array.EnumerateArray())
                    list.Add(Unwrap(item));

                break;
            case System.Collections.IEnumerable enumerable:
                foreach (var item in enumerable)
                    list.Add(Unwrap(item));

                break;
            default:
                list.Add(value);
                break;
        }

        if (list.Count == 0)
            throw new SqlTranslationException("In requires at least one value.");

        return list;
    }

    private static string LikePattern(string value, ComparisonOperatorEnum comparison)
    {
        var escaped = EscapeLike(value);
        return comparison switch {
            ComparisonOperatorEnum.StartsWith or ComparisonOperatorEnum.NotStartsWith => escaped + "%",
            ComparisonOperatorEnum.EndsWith or ComparisonOperatorEnum.NotEndsWith => "%" + escaped,
            _ => "%" + escaped + "%"
        };
    }

    private static string EscapeLike(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value) {
            if (ch is '%' or '_' or '\\')
                builder.Append('\\');

            builder.Append(ch);
        }

        return builder.ToString();
    }

    private static bool IsLike(ComparisonOperatorEnum comparison)
        => comparison is ComparisonOperatorEnum.Contains or ComparisonOperatorEnum.NotContains or ComparisonOperatorEnum.StartsWith or ComparisonOperatorEnum.NotStartsWith
            or ComparisonOperatorEnum.EndsWith or ComparisonOperatorEnum.NotEndsWith;

    private static bool IsNegatedLike(ComparisonOperatorEnum comparison)
        => comparison is ComparisonOperatorEnum.NotContains or ComparisonOperatorEnum.NotStartsWith or ComparisonOperatorEnum.NotEndsWith;

    private static string Column(SqlColumnMap map, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new SqlTranslationException("Column path is required.");

        if (field.Contains('.'))
            throw new SqlTranslationException($"Path '{field}' is not a scalar column.");

        if (!map.Columns.TryGetValue(field, out var column) || string.IsNullOrWhiteSpace(column))
            throw new SqlTranslationException($"Unknown column '{field}'.");

        return Quote(column);
    }

    private static string Qualify(SqlAlias source, string field)
    {
        if (field.Contains('.'))
            throw new SqlTranslationException($"Path '{field}' is not a scalar column.");

        if (!source.Map.Columns.TryGetValue(field, out var column))
            throw new SqlTranslationException($"Unknown column '{field}'.");

        return $"{Quote(source.Alias)}.{Quote(column)}";
    }

    private static string RootColumn(string path, Dictionary<string, SqlAlias> aliases)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new SqlTranslationException("Column path is required.");

        var dot = path.IndexOf('.');
        if (dot <= 0 || dot != path.LastIndexOf('.'))
            throw new SqlTranslationException($"Path '{path}' is not a scalar alias.property column.");

        var alias = path[..dot];
        var property = path[(dot + 1)..];
        if (!aliases.TryGetValue(alias, out var source))
            throw new SqlTranslationException($"Unknown alias '{alias}'.");

        if (!source.Map.Columns.TryGetValue(property, out var column))
            throw new SqlTranslationException($"Unknown column '{property}'.");

        return $"{Quote(alias)}.{Quote(column)}";
    }

    private static void AppendOrder(StringBuilder sql, IReadOnlyList<SortBy> sortBy, Func<string, string> column)
    {
        if (sortBy.Count == 0)
            return;

        var ordered = sortBy.Select((sort, index) => (sort, index)).OrderBy(item => item.sort.Priority ?? int.MaxValue).ThenBy(item => item.index);
        sql.Append(" ORDER BY ");
        sql.Append(string.Join(", ", ordered.Select(item => $"{column(item.sort.PropertyName)} {Direction(item.sort.Direction)}")));
    }

    private static string Direction(SortDirection? direction) => direction == SortDirection.Asc ? "ASC" : "DESC";

    private static void AppendPage(StringBuilder sql, int? start, int? amount)
    {
        if (start is < 0)
            throw new SqlTranslationException("Start cannot be negative.");

        if (amount is < 0)
            throw new SqlTranslationException("Amount cannot be negative.");

        if (start is not null)
            sql.Append(" OFFSET ").Append(start.Value);

        if (amount is not null)
            sql.Append(" LIMIT ").Append(amount.Value);
    }

    private static void AppendWhere(StringBuilder sql, string? predicate)
    {
        if (string.IsNullOrEmpty(predicate))
            return;

        sql.Append(" WHERE ");
        sql.Append(predicate);
    }

    private static string? Combine(IReadOnlyList<string> parts)
    {
        if (parts.Count == 0)
            return null;

        if (parts.Count == 1)
            return parts[0];

        return string.Join(" AND ", parts.Select(part => $"({part})"));
    }

    private static string Table(SqlColumnMap map) => string.IsNullOrEmpty(map.Schema) ? Quote(map.Table) : $"{Quote(map.Schema)}.{Quote(map.Table)}";

    private static string AliasedTable(SqlAlias source) => $"{Table(source.Map)} AS {Quote(source.Alias)}";

    private static string JoinKeyword(JoinType type)
        => type switch {
            JoinType.Inner => "INNER JOIN",
            JoinType.Left => "LEFT JOIN",
            JoinType.Right => "RIGHT JOIN",
            JoinType.FullOuter => "FULL OUTER JOIN",
            _ => throw new SqlTranslationException($"Join type '{type}' is not translated to SQL.")
        };

    private static string Quote(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new SqlTranslationException("SQL identifier is empty.");

        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static void RejectShared(QueryRequestBase request)
    {
        if (request.Include.Count > 0)
            throw new SqlTranslationException("Include is not translated to SQL.");

        if (request.Keys.Count > 0)
            throw new SqlTranslationException("Keys are not translated to SQL.");
    }

    private static void RejectComputed(IReadOnlyList<ComputedField> fields)
    {
        if (fields.Count > 0)
            throw new SqlTranslationException($"Computed field '{fields[0].Name}' is not translated to SQL.");
    }

    private static bool IsNull(object? value)
        => value is null || value is System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined };

    private static object? Unwrap(object? value)
    {
        if (value is not System.Text.Json.JsonElement element)
            return value;

        return element.ValueKind switch {
            System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined => null,
            System.Text.Json.JsonValueKind.String => element.GetString(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Number when element.TryGetInt64(out var whole) => whole,
            System.Text.Json.JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
            System.Text.Json.JsonValueKind.Number => element.GetDouble(),
            _ => element
        };
    }

    private static SqlQuery Finish(StringBuilder sql, ParameterList parameters) => new(sql.ToString(), parameters.ToArray());

    private sealed record SqlAlias(string Alias, SqlColumnMap Map);

    private sealed class ParameterList
    {
        private readonly List<object?> _values = [];

        public string Add(object? value)
        {
            _values.Add(value);
            return "$" + _values.Count;
        }

        public string ReplaceLast(object? value)
        {
            _values[^1] = value;
            return "$" + _values.Count;
        }

        public SqlParameter[] ToArray()
        {
            var parameters = new SqlParameter[_values.Count];
            for (var i = 0; i < _values.Count; i++)
                parameters[i] = new("$" + (i + 1), _values[i]);

            return parameters;
        }
    }
}
