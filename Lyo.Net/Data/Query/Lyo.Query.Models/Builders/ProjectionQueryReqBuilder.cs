using System.Diagnostics;
using System.Linq.Expressions;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Builders;

/// <summary>Fluent builder for <see cref="ProjectionQueryReq" /> (<c>/QueryProject</c> — projection + optional computed columns).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ProjectionQueryReqBuilder(ProjectionQueryReq? baseQuery = null)
{
    private readonly ProjectionQueryReq _query = baseQuery ?? new ProjectionQueryReq();

    /// <summary>Adds include paths (rare for QueryProject; API may derive includes from selects).</summary>
    public ProjectionQueryReqBuilder AddIncludes(string include, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(include);
        _query.Include.Add(include.Trim());
        foreach (var i in rest)
            _query.Include.Add(i.Trim());

        return this;
    }

    /// <summary>Adds include paths from an array.</summary>
    public ProjectionQueryReqBuilder AddIncludes(string[] includes)
    {
        ArgumentHelpers.ThrowIfNull(includes);
        ArgumentHelpers.ThrowIf(includes.Length == 0, "At least one include is required");
        foreach (var i in includes)
            _query.Include.Add(i.Trim());

        return this;
    }

    /// <inheritdoc cref="QueryReqBuilder.AddIncludes(System.Enum)" path="/summary" />
    public ProjectionQueryReqBuilder AddIncludes(Enum include)
    {
        foreach (var i in include.ToString().Split([','], StringSplitOptions.RemoveEmptyEntries))
            _query.Include.Add(i.Trim());

        return this;
    }

    /// <summary>Adds one or more projected field paths (<see cref="ProjectionQueryReq.Select" />).</summary>
    public ProjectionQueryReqBuilder AddSelects(string field, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(field);
        _query.Select.Add(field.Trim());
        foreach (var f in rest)
            _query.Select.Add(f.Trim());

        return this;
    }

    /// <summary>Adds projected fields from an array (at least one required).</summary>
    public ProjectionQueryReqBuilder AddSelects(string[] fields)
    {
        ArgumentHelpers.ThrowIfNull(fields);
        ArgumentHelpers.ThrowIf(fields.Length == 0, "At least one select field is required", nameof(fields));
        foreach (var f in fields)
            _query.Select.Add(f.Trim());

        return this;
    }

    /// <summary>Adds a SmartFormat computed column.</summary>
    public ProjectionQueryReqBuilder AddComputedField(string name, string template)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template);
        _query.ComputedFields.Add(new(name, template));
        return this;
    }

    /// <summary>Adds a pre-built <see cref="ComputedField" />.</summary>
    public ProjectionQueryReqBuilder AddComputedField(ComputedField computedField)
    {
        ArgumentHelpers.ThrowIfNull(computedField);
        _query.ComputedFields.Add(computedField);
        return this;
    }

    /// <inheritdoc cref="QueryReqBuilder.AddWhere(WhereClause)" path="/summary" />
    public ProjectionQueryReqBuilder AddWhere(WhereClause whereClause)
    {
        _query.WhereClause = whereClause;
        return this;
    }

    /// <inheritdoc cref="QueryReqBuilder.AddWhere(System.Action{WhereClauseBuilder})" path="/summary" />
    public ProjectionQueryReqBuilder AddWhere(Action<WhereClauseBuilder> configure)
    {
        var qb = WhereClauseBuilder.And();
        configure(qb);
        _query.WhereClause = qb.Build();
        return this;
    }

    /// <summary>Starts a typed builder for expression-based selects, includes, where, and sort (see <see cref="ProjectionQueryReqForBuilder{T}" />).</summary>
    public ProjectionQueryReqForBuilder<T> For<T>() => new(this);

    /// <inheritdoc cref="QueryReqBuilder.AddSort(SortBy)" path="/summary" />
    public ProjectionQueryReqBuilder AddSort(SortBy sortBy)
    {
        _query.SortBy.Add(sortBy);
        return this;
    }

    /// <inheritdoc cref="QueryReqBuilder.AddSort(string, SortDirection, int?)" path="/summary" />
    public ProjectionQueryReqBuilder AddSort(string propertyName, SortDirection direction = SortDirection.Desc, int? priority = null)
        => AddSort(new(propertyName, direction, priority));

    /// <inheritdoc cref="QueryReqBuilder.SetPagination" path="/summary" />
    public ProjectionQueryReqBuilder SetPagination(int start, int amount)
    {
        _query.Start = start;
        _query.Amount = amount;
        return this;
    }

    /// <inheritdoc cref="QueryReqBuilder.First" path="/summary" />
    public ProjectionQueryReqBuilder First() => SetPagination(0, 1);

    /// <inheritdoc cref="QueryReqBuilder.SetTotalCountMode" path="/summary" />
    public ProjectionQueryReqBuilder SetTotalCountMode(QueryTotalCountMode totalCountMode)
    {
        _query.Options.TotalCountMode = totalCountMode;
        return this;
    }

    /// <inheritdoc cref="QueryReqBuilder.SetIncludeFilterMode" path="/summary" />
    public ProjectionQueryReqBuilder SetIncludeFilterMode(QueryIncludeFilterMode includeFilterMode)
    {
        _query.Options.IncludeFilterMode = includeFilterMode;
        return this;
    }

    /// <summary>When <c>true</c>, QueryProject zips sibling fields under the same collection into one array of objects. <c>null</c> uses API default (zip).</summary>
    public ProjectionQueryReqBuilder SetZipSiblingCollectionSelections(bool? zipSiblingCollectionSelections)
    {
        _query.Options.ZipSiblingCollectionSelections = zipSiblingCollectionSelections ?? true;
        return this;
    }

    /// <summary>Returns the configured <see cref="ProjectionQueryReq" />.</summary>
    public ProjectionQueryReq Build() => _query;

    /// <summary>Creates a new empty projection builder.</summary>
    public static ProjectionQueryReqBuilder New() => new();

    public override string ToString() => _query.ToString();

    /// <summary>Typed façade over <see cref="ProjectionQueryReqBuilder" /> for expression-based select/include/where/sort.</summary>
    public class ProjectionQueryReqForBuilder<T>
    {
        private readonly ProjectionQueryReqBuilder _parent;

        public ProjectionQueryReqForBuilder(ProjectionQueryReqBuilder parent) => _parent = parent;

        /// <summary>Adds an include path from a member-access lambda.</summary>
        public ProjectionQueryReqForBuilder<T> Include(Expression<Func<T, object?>> selector)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddIncludes(path);

            return this;
        }

        /// <summary>Adds a projected field path from a member-access (or Count) lambda.</summary>
        public ProjectionQueryReqForBuilder<T> Select(Expression<Func<T, object?>> selector)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddSelects(path);

            return this;
        }

        /// <summary>Builds a typed where clause and assigns it to the projection request.</summary>
        public ProjectionQueryReqForBuilder<T> AddWhere(Action<WhereClauseBuilder.WhereClauseBuilderFor<T>> configure)
        {
            var qb = WhereClauseBuilder.And();
            var typed = qb.For<T>();
            configure(typed);
            var node = qb.Build();
            _parent.AddWhere(node);
            return this;
        }

        /// <summary>Adds a computed SmartFormat column (<see cref="ProjectionQueryReqBuilder.AddComputedField(string, string)" />).</summary>
        public ProjectionQueryReqForBuilder<T> AddComputedField(string name, string template)
        {
            _parent.AddComputedField(name, template);
            return this;
        }

        /// <summary>Appends a <see cref="SortBy" /> entry.</summary>
        public ProjectionQueryReqForBuilder<T> AddSort(SortBy sortBy)
        {
            _parent.AddSort(sortBy);
            return this;
        }

        /// <summary>Appends a sort using a property path from <paramref name="selector" />.</summary>
        public ProjectionQueryReqForBuilder<T> AddSort(Expression<Func<T, object?>> selector, SortDirection direction = SortDirection.Desc, int? priority = null)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddSort(path, direction, priority);

            return this;
        }

        /// <summary>Returns the parent <see cref="ProjectionQueryReqBuilder" />.</summary>
        public ProjectionQueryReqBuilder Done() => _parent;
    }
}