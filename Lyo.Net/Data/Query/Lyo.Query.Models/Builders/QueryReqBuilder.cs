using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Builders;

/// <summary>Fluent builder for root <see cref="QueryReq" /> (<c>/Query</c> — From/Joins + sparse Select).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QueryReqBuilder(QueryReq? baseQuery = null)
{
    private readonly QueryReq _query = baseQuery ?? new QueryReq();

    public QueryReqBuilder From(string alias, string entityType, Action<SourceQueryScope>? configureQuery = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(alias);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        _query.From = new FromClause { Alias = alias.Trim(), EntityType = entityType.Trim() };
        if (configureQuery != null) {
            var scope = new SourceQueryScope();
            configureQuery(scope);
            _query.From.Query = scope;
        }

        return this;
    }

    public QueryReqBuilder From(FromClause from)
    {
        ArgumentHelpers.ThrowIfNull(from);
        _query.From = from;
        return this;
    }

    public QueryReqBuilder Join(
        string alias,
        string entityType,
        JoinType type,
        Action<List<JoinOn>> configureOn,
        string? asName = null,
        Action<SourceQueryScope>? configureQuery = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(alias);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentHelpers.ThrowIfNull(configureOn);
        var on = new List<JoinOn>();
        configureOn(on);
        ArgumentHelpers.ThrowIf(on.Count == 0, "Join requires at least one ON clause");
        var join = new JoinClause {
            Alias = alias.Trim(),
            EntityType = entityType.Trim(),
            Type = type,
            On = on,
            As = asName
        };
        if (configureQuery != null) {
            var scope = new SourceQueryScope();
            configureQuery(scope);
            join.Query = scope;
        }

        _query.Joins.Add(join);
        return this;
    }

    public QueryReqBuilder Join(JoinClause join)
    {
        ArgumentHelpers.ThrowIfNull(join);
        _query.Joins.Add(join);
        return this;
    }

    public QueryReqBuilder AddSelects(string field, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(field);
        _query.Select.Add(field.Trim());
        foreach (var f in rest)
            _query.Select.Add(f.Trim());

        return this;
    }

    public QueryReqBuilder AddSelects(string[] fields)
    {
        ArgumentHelpers.ThrowIfNull(fields);
        ArgumentHelpers.ThrowIf(fields.Length == 0, "At least one select field is required", nameof(fields));
        foreach (var f in fields)
            _query.Select.Add(f.Trim());

        return this;
    }

    public QueryReqBuilder AddComputedField(string name, string template)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        _query.ComputedFields.Add(new(name, template));
        return this;
    }

    public QueryReqBuilder AddWhere(WhereClause whereClause)
    {
        _query.WhereClause = whereClause;
        return this;
    }

    public QueryReqBuilder AddWhere(Action<WhereClauseBuilder> configure)
    {
        var qb = WhereClauseBuilder.And();
        configure(qb);
        _query.WhereClause = qb.Build();
        return this;
    }

    public QueryReqBuilder AddSort(SortBy sortBy)
    {
        _query.SortBy.Add(sortBy);
        return this;
    }

    public QueryReqBuilder AddSort(string propertyName, SortDirection direction = SortDirection.Desc, int? priority = null)
        => AddSort(new(propertyName, direction, priority));

    /// <summary>Appends one primary-key row (single or composite parts).</summary>
    public QueryReqBuilder AddKey(params object[] keyParts)
    {
        ArgumentHelpers.ThrowIfNull(keyParts);
        ArgumentHelpers.ThrowIf(keyParts.Length == 0, "At least one key part is required", nameof(keyParts));
        _query.Keys.Add(keyParts);
        return this;
    }

    /// <summary>Appends one or more primary-key rows.</summary>
    public QueryReqBuilder AddKeys(object[] key, params object[][] rest)
    {
        ArgumentHelpers.ThrowIfNull(key);
        ArgumentHelpers.ThrowIf(key.Length == 0, "At least one key part is required", nameof(key));
        _query.Keys.Add(key);
        foreach (var row in rest) {
            ArgumentHelpers.ThrowIfNull(row);
            ArgumentHelpers.ThrowIf(row.Length == 0, "At least one key part is required", nameof(rest));
            _query.Keys.Add(row);
        }

        return this;
    }

    /// <summary>Appends primary-key rows from a sequence.</summary>
    public QueryReqBuilder AddKeys(IEnumerable<object[]> keys)
    {
        ArgumentHelpers.ThrowIfNull(keys);
        foreach (var key in keys) {
            ArgumentHelpers.ThrowIfNull(key);
            ArgumentHelpers.ThrowIf(key.Length == 0, "At least one key part is required", nameof(keys));
            _query.Keys.Add(key);
        }

        return this;
    }

    public QueryReqBuilder SetPagination(int start, int amount)
    {
        _query.Start = start;
        _query.Amount = amount;
        return this;
    }

    public QueryReqBuilder First() => SetPagination(0, 1);

    public QueryReqBuilder SetTotalCountMode(QueryTotalCountMode totalCountMode)
    {
        _query.Options.TotalCountMode = totalCountMode;
        return this;
    }

    public QueryReqBuilder SetZipSiblingCollectionSelections(bool zipSiblingCollectionSelections)
    {
        _query.Options.ZipSiblingCollectionSelections = zipSiblingCollectionSelections;
        return this;
    }

    public QueryReq Build() => _query;

    public static QueryReqBuilder New() => new();

    public override string ToString() => _query.ToString();
}
