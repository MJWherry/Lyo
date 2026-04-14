using System.Diagnostics;
using System.Linq.Expressions;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Builders;

/// <summary>Fluent builder for <see cref="QueryReq" /> (<c>/Query</c> — full entities, no sparse projection).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class QueryReqBuilder(QueryReq? baseQuery = null)
{
    private readonly QueryReq _query = baseQuery ?? new QueryReq();

    public QueryReqBuilder AddIncludes(string include, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(include, nameof(include));
        _query.Include.Add(include.Trim());
        foreach (var i in rest)
            _query.Include.Add(i.Trim());

        return this;
    }

    public QueryReqBuilder AddIncludes(string[] includes)
    {
        ArgumentHelpers.ThrowIfNull(includes, nameof(includes));
        if (includes.Length == 0)
            throw new ArgumentException("At least one include is required", nameof(includes));

        foreach (var i in includes)
            _query.Include.Add(i.Trim());

        return this;
    }

    public QueryReqBuilder AddIncludes(Enum include)
    {
        foreach (var i in include.ToString().Split([','], StringSplitOptions.RemoveEmptyEntries))
            _query.Include.Add(i.Trim());

        return this;
    }

    public QueryReqBuilder AddQuery(WhereClause whereClause)
    {
        _query.WhereClause = whereClause;
        return this;
    }

    public QueryReqBuilder AddQuery(Action<WhereClauseBuilder> configure)
    {
        var qb = WhereClauseBuilder.And();
        configure(qb);
        _query.WhereClause = qb.Build();
        return this;
    }

    public QueryReqForBuilder<T> For<T>() => new(this);

    public QueryReqBuilder AddSort(SortBy sortBy)
    {
        _query.SortBy.Add(sortBy);
        return this;
    }

    public QueryReqBuilder AddSort(string propertyName, SortDirection direction = SortDirection.Desc, int? priority = null) => AddSort(new(propertyName, direction, priority));

    public QueryReqBuilder SetPagination(int start, int amount)
    {
        _query.Start = start;
        _query.Amount = amount;
        return this;
    }

    public QueryReqBuilder First() => SetPagination(0, 1);

    public QueryReqBuilder SetTotalCountMode(QueryTotalCountMode totalCountMode)
    {
        _query.Options ??= new();
        _query.Options.TotalCountMode = totalCountMode;
        return this;
    }

    public QueryReqBuilder SetIncludeFilterMode(QueryIncludeFilterMode includeFilterMode)
    {
        _query.Options ??= new();
        _query.Options.IncludeFilterMode = includeFilterMode;
        return this;
    }

    public QueryReq Build() => _query;

    public static QueryReqBuilder New() => new();

    public override string ToString() => _query.ToString();

    public class QueryReqForBuilder<T>
    {
        private readonly QueryReqBuilder _parent;

        public QueryReqForBuilder(QueryReqBuilder parent) => _parent = parent;

        public QueryReqForBuilder<T> Include(Expression<Func<T, object?>> selector)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddIncludes(path);

            return this;
        }

        public QueryReqForBuilder<T> AddQuery(Action<WhereClauseBuilder.WhereClauseBuilderFor<T>> configure)
        {
            var qb = WhereClauseBuilder.And();
            var typed = qb.For<T>();
            configure(typed);
            var node = qb.Build();
            _parent.AddQuery(node);
            return this;
        }

        public QueryReqForBuilder<T> AddSort(SortBy sortBy)
        {
            _parent.AddSort(sortBy);
            return this;
        }

        public QueryReqForBuilder<T> AddSort(Expression<Func<T, object?>> selector, SortDirection direction = SortDirection.Desc, int? priority = null)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddSort(path, direction, priority);

            return this;
        }

        public QueryReqBuilder Done() => _parent;
    }
}
