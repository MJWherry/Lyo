using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Lyo.Exceptions;
using Lyo.Query.Models.Attributes;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Builders;

[DebuggerDisplay("{ToString(),nq}")]
public class WhereClauseBuilder
{
    private readonly List<WhereClause> _children = [];
    private readonly GroupOperatorEnum _groupOperator;
    private string? _description;
    private WhereClause? _subWhere;

    private WhereClauseBuilder(GroupOperatorEnum op, string? description = null)
    {
        _groupOperator = op;
        _description = description;
    }

    public WhereClauseBuilder SetDescription(string description)
    {
        _description = description;
        return this;
    }

    public WhereClauseBuilder AddCondition(string field, ComparisonOperatorEnum op, object? value, string? description = null)
    {
        _children.Add(new ConditionClause(field, op, value, description));
        return this;
    }

    /// <summary>Adds a condition with a SubQuery for two-phase execution (root in DB, subquery in-memory).</summary>
    public WhereClauseBuilder AddConditionWithSubClause(string field, ComparisonOperatorEnum op, object? value, Action<WhereClauseBuilder> configureSubClause, string? description = null)
    {
        var subBuilder = And();
        configureSubClause(subBuilder);
        var c = new ConditionClause(field, op, value, description);
        c.SubClause = subBuilder.Build();
        _children.Add(c);
        return this;
    }

    public WhereClauseBuilder Equals(string field, object? value, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.Equals, value, description));
        return this;
    }

    public WhereClauseBuilder NotEquals(string field, object? value, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.NotEquals, value, description));
        return this;
    }

    public WhereClauseBuilder GreaterThan(string field, object value, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.GreaterThan, value, description));
        return this;
    }

    public WhereClauseBuilder GreaterThanOrEqual(string field, object value, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.GreaterThanOrEqual, value, description));
        return this;
    }

    public WhereClauseBuilder LessThan(string field, object value, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.LessThan, value, description));
        return this;
    }

    public WhereClauseBuilder LessThanOrEqual(string field, object value, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.LessThanOrEqual, value, description));
        return this;
    }

    public WhereClauseBuilder Contains(string field, params object?[] values)
    {
        foreach (var value in values)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.Contains, value));

        return this;
    }

    public WhereClauseBuilder NotContains(string field, params object?[] values)
    {
        foreach (var value in values)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.NotContains, value));

        return this;
    }

    public WhereClauseBuilder StartsWith(string field, params string[] values)
    {
        foreach (var value in values)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.StartsWith, value));

        return this;
    }

    public WhereClauseBuilder NotStartsWith(string field, params string[] values)
    {
        foreach (var value in values)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.NotStartsWith, value));

        return this;
    }

    public WhereClauseBuilder EndsWith(string field, params string[] values)
    {
        foreach (var value in values)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.EndsWith, value));

        return this;
    }

    public WhereClauseBuilder NotEndsWith(string field, params string[] values)
    {
        foreach (var value in values)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.NotEndsWith, value));

        return this;
    }

    public WhereClauseBuilder In(string field, IEnumerable values, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.In, values, description));
        return this;
    }

    public WhereClauseBuilder In<T>(string field, params T[] values)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.In, values));
        return this;
    }

    public WhereClauseBuilder NotIn(string field, IEnumerable values, string? description = null)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.NotIn, values, description));
        return this;
    }

    public WhereClauseBuilder NotIn<T>(string field, params T[] values)
    {
        _children.Add(new ConditionClause(field, ComparisonOperatorEnum.NotIn, values));
        return this;
    }

    public WhereClauseBuilder Regex(string field, params string[] patterns)
    {
        foreach (var pattern in patterns)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.Regex, pattern));

        return this;
    }

    public WhereClauseBuilder NotRegex(string field, params string[] patterns)
    {
        foreach (var pattern in patterns)
            _children.Add(new ConditionClause(field, ComparisonOperatorEnum.NotRegex, pattern));

        return this;
    }

    public WhereClauseBuilder Add(WhereClause node)
    {
        _children.Add(node);
        return this;
    }

    public WhereClauseBuilder AddAnd(Action<WhereClauseBuilder> configure)
    {
        var builder = new WhereClauseBuilder(GroupOperatorEnum.And);
        configure(builder);
        var node = builder.Build();
        if (_groupOperator == GroupOperatorEnum.And) {
            // same operator as parent: just append (wrap single child into GroupClause if necessary)
            if (node is GroupClause ln && ln.Operator == GroupOperatorEnum.And) {
                foreach (var c in ln.Children)
                    _children.Add(c);
            }
            else
                _children.Add(node);

            return this;
        }

        // different operator: create left-associative grouping
        var left = _children.Count switch {
            0 => node,
            1 => _children[0],
            var _ => new GroupClause(_groupOperator, [.._children])
        };

        var combined = new GroupClause(GroupOperatorEnum.And, _description, left, node);
        // replace existing children with the combined node
        _children.Clear();
        _children.Add(combined);
        return this;
    }

    public WhereClauseBuilder AddOr(Action<WhereClauseBuilder> configure)
    {
        var builder = new WhereClauseBuilder(GroupOperatorEnum.Or);
        configure(builder);
        var node = builder.Build();
        if (_groupOperator == GroupOperatorEnum.Or) {
            if (node is GroupClause ln && ln.Operator == GroupOperatorEnum.Or) {
                foreach (var c in ln.Children)
                    _children.Add(c);
            }
            else
                _children.Add(node);

            return this;
        }

        // different operator: left-associative grouping
        var left = _children.Count switch {
            0 => node,
            1 => _children[0],
            var _ => new GroupClause(_groupOperator, [.._children])
        };

        var combined = new GroupClause(GroupOperatorEnum.Or, _description, left, node);
        _children.Clear();
        _children.Add(combined);
        return this;
    }

    // New explicit grouping API: allows callers to create explicit grouped nodes.
    public WhereClauseBuilder AddGroup(GroupOperatorEnum op, Action<WhereClauseBuilder> configure)
    {
        var builder = new WhereClauseBuilder(op);
        configure(builder);
        var node = builder.Build();
        WhereClause toAdd;
        if (node is GroupClause ln && ln.Operator == op)
            toAdd = ln;
        else
            toAdd = new GroupClause(op, _description, node);

        _children.Add(toAdd);
        return this;
    }

    public WhereClauseBuilder AddGroupAnd(Action<WhereClauseBuilder> configure) => AddGroup(GroupOperatorEnum.And, configure);

    public WhereClauseBuilder AddGroupOr(Action<WhereClauseBuilder> configure) => AddGroup(GroupOperatorEnum.Or, configure);

    /// <summary>Adds a SubQuery for two-phase execution (root in DB, subquery in-memory).</summary>
    public WhereClauseBuilder AddSubClause(Action<WhereClauseBuilder> configure)
    {
        var subBuilder = And();
        configure(subBuilder);
        _subWhere = subBuilder.Build();
        return this;
    }

    // Returns a grouped WhereClause using this builder's operator (does not add to children)
    public WhereClause Group(Action<WhereClauseBuilder> configure)
    {
        var builder = new WhereClauseBuilder(_groupOperator);
        configure(builder);
        var node = builder.Build();
        if (node is GroupClause ln && ln.Operator == _groupOperator)
            return ln;

        return new GroupClause(_groupOperator, _description, node);
    }

    // Explicit combine helper
    public static WhereClause CombineAs(GroupOperatorEnum op, params WhereClause[] nodes) => new GroupClause(op, nodes);

    public static WhereClauseBuilder And() => new(GroupOperatorEnum.And);

    public static WhereClauseBuilder Or() => new(GroupOperatorEnum.Or);

    public WhereClauseBuilderFor<T> For<T>() => new(this);

    public WhereClause Build()
    {
        OperationHelpers.ThrowIf(_children.Count == 0, "Cannot build query with no conditions");

        // Always wrap children into a GroupClause using this builder's operator.
        // Do not flatten a single child — keep the grouping explicit.
        var node = new GroupClause(_groupOperator, [.._children]);
        if (_subWhere != null)
            node.SubClause = _subWhere;

        return node;
    }

    public static ConditionClause Condition(string field, ComparisonOperatorEnum op, object? value, string? description = null) => new(field, op, value, description);

    /// <summary>Creates a ConditionClause with an optional SubQuery for two-phase execution (root in DB, subquery in-memory).</summary>
    public static ConditionClause ConditionWithSubClause(string field, ComparisonOperatorEnum op, object? value, WhereClause subQuery, string? description = null)
    {
        var c = new ConditionClause(field, op, value, description);
        c.SubClause = subQuery;
        return c;
    }

    public static WhereClause And(Action<WhereClauseBuilder> configure)
    {
        var builder = new WhereClauseBuilder(GroupOperatorEnum.And);
        configure(builder);
        return builder.Build();
    }

    public static WhereClause Or(Action<WhereClauseBuilder> configure)
    {
        var builder = new WhereClauseBuilder(GroupOperatorEnum.Or);
        configure(builder);
        return builder.Build();
    }

    /// <summary>Builds a WhereClause from condition nodes. If searchProperty and searchText are provided, adds a Contains condition.</summary>
    public static WhereClause? FromConditions(IEnumerable<ConditionClause> conditions, string? searchProperty = null, string? searchText = null)
    {
        var nodes = new List<WhereClause>();
        foreach (var c in conditions) {
            if (string.IsNullOrWhiteSpace(c.Field))
                continue;

            nodes.Add(c);
        }

        if (searchProperty != null && !string.IsNullOrEmpty(searchText))
            nodes.Add(new ConditionClause(searchProperty, ComparisonOperatorEnum.Contains, searchText));

        if (nodes.Count == 0)
            return null;

        if (nodes.Count == 1)
            return nodes[0];

        return new GroupClause(GroupOperatorEnum.And, nodes);
    }

    public override string ToString() => $"{string.Join($" {_groupOperator.ToString().ToUpperInvariant()} ", _children)}";

    public class WhereClauseBuilderFor<T>(WhereClauseBuilder parent)
    {
        private static string PathFrom<TProp>(Expression<Func<T, TProp>> selector) => ExpressionPropertyPath.GetPropertyPath(selector);

        public WhereClauseBuilderFor<T> AddCondition<TProp>(Expression<Func<T, TProp>> selector, ComparisonOperatorEnum op, object? value)
        {
            var path = PathFrom(selector);
            parent.AddCondition(path, op, value);
            return this;
        }

        /// <summary>Adds a condition with a SubQuery for two-phase execution (root in DB, subquery in-memory).</summary>
        public WhereClauseBuilderFor<T> AddConditionWithSubClause<TProp>(
            Expression<Func<T, TProp>> selector,
            ComparisonOperatorEnum op,
            object? value,
            Action<WhereClauseBuilderFor<T>> configureSubClause)
        {
            var path = PathFrom(selector);
            parent.AddConditionWithSubClause(
                path, op, value, b => {
                    var fb = new WhereClauseBuilderFor<T>(b);
                    configureSubClause(fb);
                });

            return this;
        }

        public WhereClauseBuilderFor<T> AddEquals<TProp>(Expression<Func<T, TProp>> selector, TProp? value) => AddCondition(selector, ComparisonOperatorEnum.Equals, value);

        public WhereClauseBuilderFor<T> NotEquals<TProp>(Expression<Func<T, TProp>> selector, TProp? value) => AddCondition(selector, ComparisonOperatorEnum.NotEquals, value);

        public WhereClauseBuilderFor<T> GreaterThan<TProp>(Expression<Func<T, TProp>> selector, TProp? value) => AddCondition(selector, ComparisonOperatorEnum.GreaterThan, value);

        public WhereClauseBuilderFor<T> GreaterThanOrEqual<TProp>(Expression<Func<T, TProp>> selector, TProp? value)
            => AddCondition(selector, ComparisonOperatorEnum.GreaterThanOrEqual, value);

        public WhereClauseBuilderFor<T> LessThan<TProp>(Expression<Func<T, TProp>> selector, TProp? value) => AddCondition(selector, ComparisonOperatorEnum.LessThan, value);

        public WhereClauseBuilderFor<T> LessThanOrEqual<TProp>(Expression<Func<T, TProp>> selector, TProp? value) => AddCondition(selector, ComparisonOperatorEnum.LessThanOrEqual, value);

        public WhereClauseBuilderFor<T> Contains(Expression<Func<T, string>> selector, params string[] values)
        {
            var path = PathFrom(selector);
            foreach (var v in values)
                parent.Contains(path, v);

            return this;
        }

        public WhereClauseBuilderFor<T> NotContains(Expression<Func<T, string>> selector, params string[] values)
        {
            var path = PathFrom(selector);
            foreach (var v in values)
                parent.NotContains(path, v);

            return this;
        }

        public WhereClauseBuilderFor<T> StartsWith(Expression<Func<T, string>> selector, params string[] values)
        {
            var path = PathFrom(selector);
            foreach (var v in values)
                parent.StartsWith(path, v);

            return this;
        }

        public WhereClauseBuilderFor<T> NotStartsWith(Expression<Func<T, string>> selector, params string[] values)
        {
            var path = PathFrom(selector);
            foreach (var v in values)
                parent.NotStartsWith(path, v);

            return this;
        }

        public WhereClauseBuilderFor<T> EndsWith(Expression<Func<T, string>> selector, params string[] values)
        {
            var path = PathFrom(selector);
            foreach (var v in values)
                parent.EndsWith(path, v);

            return this;
        }

        public WhereClauseBuilderFor<T> NotEndsWith(Expression<Func<T, string>> selector, params string[] values)
        {
            var path = PathFrom(selector);
            foreach (var v in values)
                parent.NotEndsWith(path, v);

            return this;
        }

        public WhereClauseBuilderFor<T> In<TProp>(Expression<Func<T, TProp>> selector, params TProp[] values)
        {
            var path = PathFrom(selector);
            parent.In(path, values);
            return this;
        }

        public WhereClauseBuilderFor<T> NotIn<TProp>(Expression<Func<T, TProp>> selector, params TProp[] values)
        {
            var path = PathFrom(selector);
            parent.NotIn(path, values);
            return this;
        }

        public WhereClauseBuilderFor<T> Regex(Expression<Func<T, string>> selector, string pattern)
        {
            var path = PathFrom(selector);
            parent.Regex(path, pattern);
            return this;
        }

        public WhereClauseBuilderFor<T> NotRegex(Expression<Func<T, string>> selector, string pattern)
        {
            var path = PathFrom(selector);
            parent.NotRegex(path, pattern);
            return this;
        }

        public WhereClauseBuilderFor<T> Add(WhereClause node)
        {
            parent.Add(node);
            return this;
        }

        public WhereClauseBuilderFor<T> AddAnd(Action<WhereClauseBuilderFor<T>> configure)
        {
            parent.AddAnd(b => {
                var fb = new WhereClauseBuilderFor<T>(b);
                configure(fb);
            });

            return this;
        }

        public WhereClauseBuilderFor<T> AddOr(Action<WhereClauseBuilderFor<T>> configure)
        {
            parent.AddOr(b => {
                var fb = new WhereClauseBuilderFor<T>(b);
                configure(fb);
            });

            return this;
        }

        // Typed grouping helpers
        public WhereClauseBuilderFor<T> AddGroup(GroupOperatorEnum op, Action<WhereClauseBuilderFor<T>> configure)
        {
            parent.AddGroup(
                op, b => {
                    var fb = new WhereClauseBuilderFor<T>(b);
                    configure(fb);
                });

            return this;
        }

        public WhereClauseBuilderFor<T> AddGroupAnd(Action<WhereClauseBuilderFor<T>> configure) => AddGroup(GroupOperatorEnum.And, configure);

        public WhereClauseBuilderFor<T> AddGroupOr(Action<WhereClauseBuilderFor<T>> configure) => AddGroup(GroupOperatorEnum.Or, configure);

        public WhereClauseBuilderFor<T> AddSubClause(Action<WhereClauseBuilderFor<T>> configure)
        {
            parent.AddSubClause(b => {
                var fb = new WhereClauseBuilderFor<T>(b);
                configure(fb);
            });

            return this;
        }
    }

    // Helper to extract property path from expression
    private static class ExpressionPropertyPath
    {
        public static string GetPropertyPath(LambdaExpression? expr)
        {
            if (expr == null)
                return string.Empty;

            var body = expr.Body;
            if (body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
                body = ue.Operand;

            var members = new List<string>();
            while (body is MemberExpression me) {
                // record the member (we'll resolve attribute -> column name below)
                var member = me.Member;
                // if property and has QueryPropertyNameAttribute or DatabaseColumnNameAttribute, use that for the path
                if (member is PropertyInfo pi) {
                    var queryAttr = pi.GetCustomAttribute<QueryPropertyNameAttribute>(true);
                    if (queryAttr != null && !string.IsNullOrEmpty(queryAttr.PropertyName))
                        members.Insert(0, queryAttr.PropertyName);
                    else
                        members.Insert(0, pi.Name);
                }
                else
                    members.Insert(0, me.Member.Name);

                body = me.Expression!;
                if (body is UnaryExpression u2 && u2.NodeType == ExpressionType.Convert)
                    body = u2.Operand;
            }

            return string.Join(".", members);
        }
    }
}