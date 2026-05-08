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

    /// <summary>Adds one or more include paths (database navigation names).</summary>
    public QueryReqBuilder AddIncludes(string include, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(include);
        _query.Include.Add(include.Trim());
        foreach (var i in rest)
            _query.Include.Add(i.Trim());

        return this;
    }

    /// <summary>Adds include paths from an array.</summary>
    public QueryReqBuilder AddIncludes(string[] includes)
    {
        ArgumentHelpers.ThrowIfNull(includes);
        ArgumentHelpers.ThrowIf(includes.Length == 0, "At least one include is required");
        foreach (var i in includes)
            _query.Include.Add(i.Trim());

        return this;
    }

    /// <summary>Adds include paths by splitting the enum member name on commas (flags-style).</summary>
    public QueryReqBuilder AddIncludes(Enum include)
    {
        foreach (var i in include.ToString().Split([','], StringSplitOptions.RemoveEmptyEntries))
            _query.Include.Add(i.Trim());

        return this;
    }

    /// <summary>Sets the root <see cref="QueryRequestBase.WhereClause" />.</summary>
    public QueryReqBuilder AddWhere(WhereClause whereClause)
    {
        _query.WhereClause = whereClause;
        return this;
    }

    /// <summary>Builds a where clause using <see cref="WhereClauseBuilder.And()" /> and assigns it to the request.</summary>
    public QueryReqBuilder AddWhere(Action<WhereClauseBuilder> configure)
    {
        var qb = WhereClauseBuilder.And();
        configure(qb);
        _query.WhereClause = qb.Build();
        return this;
    }

    /// <summary>Starts a typed builder that resolves includes/sorts/where from lambda expressions.</summary>
    public QueryReqForBuilder<T> For<T>() => new(this);

    /// <summary>Appends a <see cref="SortBy" /> entry.</summary>
    public QueryReqBuilder AddSort(SortBy sortBy)
    {
        _query.SortBy.Add(sortBy);
        return this;
    }

    /// <summary>Appends a sort by property name, direction, and optional priority.</summary>
    public QueryReqBuilder AddSort(string propertyName, SortDirection direction = SortDirection.Desc, int? priority = null) => AddSort(new(propertyName, direction, priority));

    /// <summary>Sets <see cref="QueryRequestBase.Start" /> and <see cref="QueryRequestBase.Amount" />.</summary>
    public QueryReqBuilder SetPagination(int start, int amount)
    {
        _query.Start = start;
        _query.Amount = amount;
        return this;
    }

    /// <summary>Requests the first row only (<c>Start=0</c>, <c>Amount=1</c>).</summary>
    public QueryReqBuilder First() => SetPagination(0, 1);

    /// <summary>Sets <see cref="QueryRequestOptions.TotalCountMode" />.</summary>
    public QueryReqBuilder SetTotalCountMode(QueryTotalCountMode totalCountMode)
    {
        _query.Options.TotalCountMode = totalCountMode;
        return this;
    }

    /// <summary>Sets <see cref="QueryRequestOptions.IncludeFilterMode" />.</summary>
    public QueryReqBuilder SetIncludeFilterMode(QueryIncludeFilterMode includeFilterMode)
    {
        _query.Options.IncludeFilterMode = includeFilterMode;
        return this;
    }

    /// <summary>Returns the configured <see cref="QueryReq" />.</summary>
    public QueryReq Build() => _query;

    /// <summary>Creates a new empty builder.</summary>
    public static QueryReqBuilder New() => new();

    public override string ToString() => _query.ToString();

    /// <summary>Typed façade over <see cref="QueryReqBuilder" /> using expression-based paths.</summary>
    public class QueryReqForBuilder<T>(QueryReqBuilder parent)
    {
        /// <summary>Adds an include derived from a member-access lambda (honors <see cref="QueryPropertyNameAttribute" />).</summary>
        public QueryReqForBuilder<T> Include(Expression<Func<T, object?>> selector)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                parent.AddIncludes(path);

            return this;
        }

        /// <summary>Builds a typed where clause and assigns it to the request.</summary>
        public QueryReqForBuilder<T> AddWhere(Action<WhereClauseBuilder.WhereClauseBuilderFor<T>> configure)
        {
            var qb = WhereClauseBuilder.And();
            var typed = qb.For<T>();
            configure(typed);
            var node = qb.Build();
            parent.AddWhere(node);
            return this;
        }

        /// <inheritdoc cref="QueryReqBuilder.AddSort(SortBy)" path="/summary" />
        public QueryReqForBuilder<T> AddSort(SortBy sortBy)
        {
            parent.AddSort(sortBy);
            return this;
        }

        /// <summary>Appends a sort keyed by a property path from <paramref name="selector" />.</summary>
        public QueryReqForBuilder<T> AddSort(Expression<Func<T, object?>> selector, SortDirection direction = SortDirection.Desc, int? priority = null)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                parent.AddSort(path, direction, priority);

            return this;
        }

        /// <summary>Returns the parent builder to continue configuration.</summary>
        public QueryReqBuilder Done() => parent;
    }
}