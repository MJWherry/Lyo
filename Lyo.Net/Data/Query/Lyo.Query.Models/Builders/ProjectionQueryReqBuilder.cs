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

    public ProjectionQueryReqBuilder AddIncludes(string include, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(include, nameof(include));
        _query.Include.Add(include.Trim());
        foreach (var i in rest)
            _query.Include.Add(i.Trim());

        return this;
    }

    public ProjectionQueryReqBuilder AddIncludes(string[] includes)
    {
        ArgumentHelpers.ThrowIfNull(includes, nameof(includes));
        if (includes.Length == 0)
            throw new ArgumentException("At least one include is required", nameof(includes));

        foreach (var i in includes)
            _query.Include.Add(i.Trim());

        return this;
    }

    public ProjectionQueryReqBuilder AddIncludes(Enum include)
    {
        foreach (var i in include.ToString().Split([','], StringSplitOptions.RemoveEmptyEntries))
            _query.Include.Add(i.Trim());

        return this;
    }

    public ProjectionQueryReqBuilder AddSelects(string field, params string[] rest)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(field, nameof(field));
        _query.Select.Add(field.Trim());
        foreach (var f in rest)
            _query.Select.Add(f.Trim());

        return this;
    }

    public ProjectionQueryReqBuilder AddSelects(string[] fields)
    {
        ArgumentHelpers.ThrowIfNull(fields, nameof(fields));
        if (fields.Length == 0)
            throw new ArgumentException("At least one select field is required", nameof(fields));

        foreach (var f in fields)
            _query.Select.Add(f.Trim());

        return this;
    }

    public ProjectionQueryReqBuilder AddComputedField(string name, string template)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(template, nameof(template));
        _query.ComputedFields.Add(new(name, template));
        return this;
    }

    public ProjectionQueryReqBuilder AddComputedField(ComputedField computedField)
    {
        ArgumentHelpers.ThrowIfNull(computedField, nameof(computedField));
        _query.ComputedFields.Add(computedField);
        return this;
    }

    public ProjectionQueryReqBuilder AddQuery(WhereClause whereClause)
    {
        _query.WhereClause = whereClause;
        return this;
    }

    public ProjectionQueryReqBuilder AddQuery(Action<WhereClauseBuilder> configure)
    {
        var qb = WhereClauseBuilder.And();
        configure(qb);
        _query.WhereClause = qb.Build();
        return this;
    }

    public ProjectionQueryReqForBuilder<T> For<T>() => new(this);

    public ProjectionQueryReqBuilder AddSort(SortBy sortBy)
    {
        _query.SortBy.Add(sortBy);
        return this;
    }

    public ProjectionQueryReqBuilder AddSort(string propertyName, SortDirection direction = SortDirection.Desc, int? priority = null) => AddSort(new(propertyName, direction, priority));

    public ProjectionQueryReqBuilder SetPagination(int start, int amount)
    {
        _query.Start = start;
        _query.Amount = amount;
        return this;
    }

    public ProjectionQueryReqBuilder First() => SetPagination(0, 1);

    public ProjectionQueryReqBuilder SetTotalCountMode(QueryTotalCountMode totalCountMode)
    {
        _query.Options ??= new();
        _query.Options.TotalCountMode = totalCountMode;
        return this;
    }

    public ProjectionQueryReqBuilder SetIncludeFilterMode(QueryIncludeFilterMode includeFilterMode)
    {
        _query.Options ??= new();
        _query.Options.IncludeFilterMode = includeFilterMode;
        return this;
    }

    /// <summary>When <c>true</c>, QueryProject zips sibling fields under the same collection into one array of objects. <c>null</c> uses API default (zip).</summary>
    public ProjectionQueryReqBuilder SetZipSiblingCollectionSelections(bool? zipSiblingCollectionSelections)
    {
        _query.Options ??= new();
        _query.Options.ZipSiblingCollectionSelections = zipSiblingCollectionSelections ?? true;
        return this;
    }

    public ProjectionQueryReq Build() => _query;

    public static ProjectionQueryReqBuilder New() => new();

    public override string ToString() => _query.ToString();

    public class ProjectionQueryReqForBuilder<T>
    {
        private readonly ProjectionQueryReqBuilder _parent;

        public ProjectionQueryReqForBuilder(ProjectionQueryReqBuilder parent) => _parent = parent;

        public ProjectionQueryReqForBuilder<T> Include(Expression<Func<T, object?>> selector)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddIncludes(path);

            return this;
        }

        public ProjectionQueryReqForBuilder<T> Select(Expression<Func<T, object?>> selector)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddSelects(path);

            return this;
        }

        public ProjectionQueryReqForBuilder<T> AddQuery(Action<WhereClauseBuilder.WhereClauseBuilderFor<T>> configure)
        {
            var qb = WhereClauseBuilder.And();
            var typed = qb.For<T>();
            configure(typed);
            var node = qb.Build();
            _parent.AddQuery(node);
            return this;
        }

        public ProjectionQueryReqForBuilder<T> AddComputedField(string name, string template)
        {
            _parent.AddComputedField(name, template);
            return this;
        }

        public ProjectionQueryReqForBuilder<T> AddSort(SortBy sortBy)
        {
            _parent.AddSort(sortBy);
            return this;
        }

        public ProjectionQueryReqForBuilder<T> AddSort(Expression<Func<T, object?>> selector, SortDirection direction = SortDirection.Desc, int? priority = null)
        {
            var path = QueryBuilderExpressionPathHelper.GetPropertyPath(selector);
            if (!string.IsNullOrEmpty(path))
                _parent.AddSort(path, direction, priority);

            return this;
        }

        public ProjectionQueryReqBuilder Done() => _parent;
    }
}
