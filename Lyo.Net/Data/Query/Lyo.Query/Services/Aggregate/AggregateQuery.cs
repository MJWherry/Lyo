using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.Path;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Services.Aggregate;

/// <summary>
/// Groups an <see cref="IQueryable{T}" /> by <see cref="ProjectionQueryReq.Select" /> and projects <see cref="ProjectionQueryReq.Aggregates" />. The result is a flat class, one property per
/// column. Npgsql reads tuple results as a PostgreSQL record, which this host cannot materialize.
/// </summary>
public static class AggregateQuery
{
    /// <summary>Most <see cref="ProjectionQueryReq.Select" /> paths in one aggregate request.</summary>
    public const int MaxGroupKeys = 8;

    /// <summary>Most <see cref="ProjectionQueryReq.Aggregates" /> in one request.</summary>
    public const int MaxAggregates = 8;

    /// <summary>Errors that make <paramref name="request" /> an invalid aggregate query for <typeparamref name="T" />. Empty when the request can run.</summary>
    public static IReadOnlyList<string> Validate<T>(ProjectionQueryReq request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var errors = new List<string>();
        if (request.Aggregates.Count == 0)
            errors.Add("Aggregates are required.");

        if (request.Keys.Count > 0)
            errors.Add("Aggregate queries cannot target keys.");

        if (request.ComputedFields.Count > 0)
            errors.Add("Aggregate queries cannot use computed fields.");

        if (WhereClauseHelpers.HasAnySubClause(request.WhereClause))
            errors.Add("Aggregate queries cannot use a sub-clause.");

        if (request.Options.IncludeFilterMode == QueryIncludeFilterMode.MatchedOnly)
            errors.Add("Aggregate queries cannot use matched-only includes.");

        if (request.Select.Count > MaxGroupKeys)
            errors.Add($"Select has more than {MaxGroupKeys} columns.");

        if (request.Aggregates.Count > MaxAggregates)
            errors.Add($"Aggregates has more than {MaxAggregates} columns.");

        var outputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groupPaths = new List<ScalarPath>();
        if (request.Select.Count <= MaxGroupKeys) {
            foreach (var select in request.Select) {
                if (!TryResolve<T>(select, "Select path", out var path, out var error)) {
                    errors.Add(error!);
                    continue;
                }

                if (!outputNames.Add(path!.Requested))
                    errors.Add($"Select path '{path.Requested}' is duplicated.");

                groupPaths.Add(path);
            }
        }

        if (request.Aggregates.Count <= MaxAggregates) {
            foreach (var aggregate in request.Aggregates)
                ValidateAggregate<T>(aggregate, outputNames, errors);
        }

        foreach (var sort in request.SortBy) {
            if (string.IsNullOrWhiteSpace(sort.PropertyName)) {
                errors.Add("Sort field is empty.");
                continue;
            }

            if (!outputNames.Contains(sort.PropertyName.Trim()))
                errors.Add($"Sort field '{sort.PropertyName}' is not a selected column or an aggregate name.");
        }

        return errors;
    }

    /// <summary>Runs <paramref name="request" /> against <paramref name="source" />. <paramref name="source" /> is already filtered. Throws <see cref="InvalidQueryException" /> when <see cref="Validate{T}" /> reports errors.</summary>
    public static AggregateQueryPage Execute<T>(IQueryable<T> source, ProjectionQueryReq request)
    {
        var errors = Validate<T>(request);
        if (errors.Count > 0)
            throw new InvalidQueryException(string.Join(" ", errors));

        var plan = Compose(source, request);
        return plan.Execute(request.Start ?? 0, request.Amount ?? int.MaxValue, request.Options.TotalCountMode);
    }

    /// <summary>Builds a translatable grouped query. Call <see cref="Validate{T}" /> first when the caller wants every error instead of the first throw.</summary>
    public static AggregateQueryPlan Compose<T>(IQueryable<T> source, ProjectionQueryReq request)
    {
        ArgumentHelpers.ThrowIfNull(source);
        var errors = Validate<T>(request);
        if (errors.Count > 0)
            throw new InvalidQueryException(string.Join(" ", errors));

        var groups = ResolveAll<T>(request.Select, "Select path");
        var aggregates = ResolveAggregates<T>(request);
        var row = BuildRow(typeof(T), groups, aggregates);
        var query = ApplySource(source, row);
        var ordered = ApplyOrdering(query, row.Shape, groups, aggregates, request.SortBy);
        var empty = aggregates.Select(a => new KeyValuePair<string, object?>(a.Name, EmptyValue(a.Function, a.FieldType))).ToArray();
        return new(ordered, row.Shape, groups.Select(g => g.Requested).ToArray(), aggregates.Select(a => a.Name).ToArray(), empty);
    }

    private static void ValidateAggregate<T>(AggregateSelect aggregate, HashSet<string> outputNames, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(aggregate.Name)) {
            errors.Add("Aggregate name is required.");
            return;
        }

        var name = aggregate.Name.Trim();
        if (!outputNames.Add(name))
            errors.Add($"Aggregate name '{name}' is duplicated.");

        var field = aggregate.Field?.Trim();
        var hasField = !string.IsNullOrEmpty(field);
        if (aggregate.Function == AggregateFunction.Count) {
            if (hasField && !TryResolve<T>(field, $"{aggregate.Function} field", countCollection: true, out _, out var countError))
                errors.Add(countError!);

            return;
        }

        if (!hasField) {
            errors.Add($"{aggregate.Function} requires a field.");
            return;
        }

        if (!TryResolve<T>(field!, $"{aggregate.Function} field", out var path, out var error)) {
            errors.Add(error!);
            return;
        }

        if (aggregate.Function is AggregateFunction.Sum or AggregateFunction.Average && !IsNumeric(path!.Type))
            errors.Add($"{aggregate.Function} field '{path.Requested}' is not numeric.");
    }

    private static List<ScalarPath> ResolveAll<T>(IReadOnlyList<string> paths, string label)
    {
        var resolved = new List<ScalarPath>(paths.Count);
        foreach (var path in paths) {
            if (!TryResolve<T>(path, label, out var scalar, out var error))
                throw new InvalidQueryException(error);

            resolved.Add(scalar!);
        }

        return resolved;
    }

    private static List<ResolvedAggregate> ResolveAggregates<T>(ProjectionQueryReq request)
    {
        var resolved = new List<ResolvedAggregate>(request.Aggregates.Count);
        foreach (var aggregate in request.Aggregates) {
            ScalarPath? field = null;
            if (aggregate.Function != AggregateFunction.Count || !string.IsNullOrWhiteSpace(aggregate.Field)) {
                if (!TryResolve<T>(aggregate.Field, $"{aggregate.Function} field", aggregate.Function == AggregateFunction.Count, out field, out var error))
                    throw new InvalidQueryException(error);
            }

            resolved.Add(new(aggregate.Function, aggregate.Name.Trim(), field));
        }

        return resolved;
    }

    private static bool TryResolve<T>(string? path, string label, out ScalarPath? scalar, out string? error)
        => TryResolve<T>(path, label, countCollection: false, out scalar, out error);

    private static bool TryResolve<T>(string? path, string label, bool countCollection, out ScalarPath? scalar, out string? error)
    {
        scalar = null;
        if (string.IsNullOrWhiteSpace(path)) {
            error = $"{label} is empty.";
            return false;
        }

        var requested = path.Trim();
        if (!QueryPath.TryBindFactory(typeof(T), requested, requireScalar: true, countCollection, out var bind, out var type, out var collectionSize, out var reason)) {
            error = $"{label} '{requested}' {reason}";
            return false;
        }

        if (!IsScalar(type!)) {
            error = $"{label} '{requested}' is not a scalar.";
            return false;
        }

        scalar = new(requested, bind!, type!, collectionSize);
        error = null;
        return true;
    }

    private static BuiltRow BuildRow(Type elementType, List<ScalarPath> groups, List<ResolvedAggregate> aggregates)
    {
        var parameter = Expression.Parameter(elementType, "row");
        Expression keyBody = Expression.Constant(0);
        if (groups.Count == 1)
            keyBody = Access(parameter, groups[0]);
        else if (groups.Count > 1)
            keyBody = NewKey(parameter, groups);

        var keyLambda = Expression.Lambda(keyBody, parameter);
        var keyType = keyBody.Type;
        var groupType = typeof(IGrouping<,>).MakeGenericType(keyType, elementType);
        var group = Expression.Parameter(groupType, "g");
        var columns = new List<(string Name, Type Type, Expression Value)>();
        if (groups.Count == 1)
            columns.Add(("K0", groups[0].Type, Expression.Property(group, "Key")));
        else if (groups.Count > 1) {
            var key = Expression.Property(group, "Key");
            for (var i = 0; i < groups.Count; i++)
                columns.Add(("K" + i, groups[i].Type, Expression.Property(key, "K" + i)));
        }

        foreach (var aggregate in aggregates) {
            var call = AggregateCall(elementType, group, aggregate);
            columns.Add(("A" + (columns.Count - groups.Count), call.Type, call));
        }

        var shape = AggregateRowFactory.Create(columns.Select(c => (c.Name, c.Type)).ToArray());
        var bindings = columns.Select((column, index) => Expression.Bind(shape.Properties[index], column.Value));
        var rowNew = Expression.MemberInit(Expression.New(shape.Constructor), bindings);
        var selectLambda = Expression.Lambda(rowNew, group);
        return new(shape, keyLambda, selectLambda, elementType, keyType);
    }

    private static IQueryable ApplySource(IQueryable source, BuiltRow row)
    {
        var grouped = (IQueryable)Invoke(QueryableMethods.GroupBy.MakeGenericMethod(row.ElementType, row.KeyType), [source, row.KeyLambda]);
        return (IQueryable)Invoke(QueryableMethods.Select.MakeGenericMethod(grouped.ElementType, row.Shape.Type), [grouped, row.SelectLambda]);
    }

    private static Expression AggregateCall(Type elementType, ParameterExpression group, ResolvedAggregate aggregate)
    {
        if (aggregate.Function == AggregateFunction.Count) {
            if (aggregate.Field is null) {
                var count = EnumerableMethods.Count.MakeGenericMethod(elementType);
                return Expression.Call(count, group);
            }

            if (aggregate.Field.CollectionSize)
                return Expression.Call(ScalarAggregate("Sum", elementType, aggregate.Field.Type), group, FieldSelector(elementType, aggregate.Field));

            return CountValues(elementType, group, aggregate.Field, distinct: false);
        }

        if (aggregate.Function == AggregateFunction.CountDistinct)
            return CountValues(elementType, group, aggregate.Field!, distinct: true);

        var field = aggregate.Field!;
        var item = Expression.Parameter(elementType, "e");
        var selector = Expression.Lambda(Access(item, field), item);
        if (aggregate.Function is AggregateFunction.Min or AggregateFunction.Max) {
            var minMax = (aggregate.Function == AggregateFunction.Min ? EnumerableMethods.Min : EnumerableMethods.Max).MakeGenericMethod(elementType, field.Type);
            return Expression.Call(minMax, group, selector);
        }

        var method = ScalarAggregate(aggregate.Function == AggregateFunction.Sum ? "Sum" : "Average", elementType, field.Type);
        return Expression.Call(method, group, selector);
    }

    private static MethodInfo ScalarAggregate(string name, Type source, Type selected)
    {
        foreach (var method in typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (method.Name != name || !method.IsGenericMethodDefinition || method.GetGenericArguments().Length != 1)
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                continue;

            var func = parameters[1].ParameterType;
            if (!func.IsGenericType || func.GetGenericTypeDefinition() != typeof(Func<,>))
                continue;

            if (func.GetGenericArguments()[1] == selected)
                return method.MakeGenericMethod(source);
        }

        throw new InvalidQueryException($"{name} does not support '{selected.Name}'.");
    }

    private static IQueryable ApplyOrdering(IQueryable query, AggregateRowShape shape, List<ScalarPath> groups, List<ResolvedAggregate> aggregates, IReadOnlyList<SortBy> sortBy)
    {
        var sorts = sortBy.Select((sort, index) => (sort.PropertyName.Trim(), sort.Direction ?? SortDirection.Asc, sort.Priority ?? index))
            .OrderBy(s => s.Item3)
            .ThenBy(s => s.Item1, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steps = new List<(PropertyInfo Property, bool Descending)>();
        foreach (var sort in sorts) {
            used.Add(sort.Item1);
            steps.Add((PropertyFor(shape, groups, aggregates, sort.Item1), sort.Item2 == SortDirection.Desc));
        }

        for (var i = 0; i < groups.Count; i++) {
            if (used.Add(groups[i].Requested))
                steps.Add((shape.Properties[i], false));
        }

        var ordered = query;
        for (var i = 0; i < steps.Count; i++) {
            var method = (i == 0, steps[i].Descending) switch {
                (true, false) => QueryableMethods.OrderBy,
                (true, true) => QueryableMethods.OrderByDescending,
                (false, false) => QueryableMethods.ThenBy,
                _ => QueryableMethods.ThenByDescending
            };

            var parameter = Expression.Parameter(shape.Type, "row");
            var lambda = Expression.Lambda(Expression.Property(parameter, steps[i].Property), parameter);
            ordered = (IQueryable)Invoke(method.MakeGenericMethod(shape.Type, steps[i].Property.PropertyType), [ordered, lambda]);
        }

        return ordered;
    }

    private static PropertyInfo PropertyFor(AggregateRowShape shape, List<ScalarPath> groups, List<ResolvedAggregate> aggregates, string name)
    {
        for (var i = 0; i < groups.Count; i++) {
            if (string.Equals(groups[i].Requested, name, StringComparison.OrdinalIgnoreCase))
                return shape.Properties[i];
        }

        for (var i = 0; i < aggregates.Count; i++) {
            if (string.Equals(aggregates[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return shape.Properties[groups.Count + i];
        }

        throw new InvalidQueryException($"Sort field '{name}' is not a selected column or an aggregate name.");
    }

    private static Expression NewKey(ParameterExpression parameter, List<ScalarPath> groups)
    {
        var shape = AggregateRowFactory.Create(groups.Select((group, index) => ("K" + index, group.Type)).ToArray(), equality: true);
        var bindings = groups.Select((group, index) => Expression.Bind(shape.Properties[index], Access(parameter, group)));
        return Expression.MemberInit(Expression.New(shape.Constructor), bindings);
    }

    private static Expression Access(Expression root, ScalarPath path) => path.Bind(root);

    private static LambdaExpression FieldSelector(Type elementType, ScalarPath field)
    {
        var item = Expression.Parameter(elementType, "e");
        return Expression.Lambda(Access(item, field), item);
    }

    private static Expression CountValues(Type elementType, ParameterExpression group, ScalarPath field, bool distinct)
    {
        Expression source = group;
        var fieldType = field.Type;
        var dropsNull = !fieldType.IsValueType || Nullable.GetUnderlyingType(fieldType) != null;
        if (dropsNull) {
            var filtered = Expression.Parameter(elementType, "e");
            var present = Expression.NotEqual(Access(filtered, field), Expression.Constant(null, fieldType));
            var where = EnumerableMethods.Where.MakeGenericMethod(elementType);
            source = Expression.Call(where, source, Expression.Lambda(present, filtered));
        }

        var selected = Expression.Call(EnumerableMethods.Select.MakeGenericMethod(elementType, fieldType), source, FieldSelector(elementType, field));
        if (distinct)
            selected = Expression.Call(EnumerableMethods.Distinct.MakeGenericMethod(fieldType), selected);

        return Expression.Call(EnumerableMethods.Count.MakeGenericMethod(fieldType), selected);
    }

    private static object? EmptyValue(AggregateFunction function, Type? fieldType)
    {
        if (function is AggregateFunction.Count or AggregateFunction.CountDistinct)
            return 0;

        if (function != AggregateFunction.Sum || fieldType is null || Nullable.GetUnderlyingType(fieldType) != null)
            return null;

        return Activator.CreateInstance(fieldType);
    }

    private static bool IsNumeric(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal);
    }

    private static bool IsScalar(Type type) => QueryPath.IsScalar(type);

    private static object Invoke(MethodInfo method, object?[] args)
    {
        try {
            return method.Invoke(null, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null) {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private sealed class ScalarPath(string requested, Func<Expression, Expression> bind, Type type, bool collectionSize = false)
    {
        public string Requested { get; } = requested;

        public Func<Expression, Expression> Bind { get; } = bind;

        public Type Type { get; } = type;

        public bool CollectionSize { get; } = collectionSize;
    }

    private sealed class ResolvedAggregate(AggregateFunction function, string name, ScalarPath? field)
    {
        public AggregateFunction Function { get; } = function;

        public string Name { get; } = name;

        public ScalarPath? Field { get; } = field;

        public Type? FieldType => Field?.Type;
    }

    private sealed class BuiltRow(AggregateRowShape shape, LambdaExpression keyLambda, LambdaExpression selectLambda, Type elementType, Type keyType)
    {
        public AggregateRowShape Shape { get; } = shape;

        public LambdaExpression KeyLambda { get; } = keyLambda;

        public LambdaExpression SelectLambda { get; } = selectLambda;

        public Type ElementType { get; } = elementType;

        public Type KeyType { get; } = keyType;
    }

    private static class QueryableMethods
    {
        public static readonly MethodInfo GroupBy = Unique("GroupBy", 2, null);
        public static readonly MethodInfo Select = Unique("Select", 2, m => FuncArity(m) == 2);
        public static readonly MethodInfo OrderBy = Unique("OrderBy", 2, m => FuncArity(m) == 2);
        public static readonly MethodInfo OrderByDescending = Unique("OrderByDescending", 2, m => FuncArity(m) == 2);
        public static readonly MethodInfo ThenBy = Unique("ThenBy", 2, m => FuncArity(m) == 2);
        public static readonly MethodInfo ThenByDescending = Unique("ThenByDescending", 2, m => FuncArity(m) == 2);

        private static MethodInfo Unique(string name, int parameters, Func<MethodInfo, bool>? match)
        {
            var methods = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name && m.IsGenericMethodDefinition && m.GetParameters().Length == parameters);

            if (match is not null)
                methods = methods.Where(match);

            return methods.Single();
        }

        private static int FuncArity(MethodInfo method)
        {
            var expression = method.GetParameters()[1].ParameterType;
            return expression.GetGenericArguments()[0].GetGenericArguments().Length;
        }
    }

    private static class EnumerableMethods
    {
        public static readonly MethodInfo Count = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Enumerable.Count) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

        public static readonly MethodInfo Where = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Enumerable.Where) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);

        public static readonly MethodInfo Select = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Enumerable.Select) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);

        public static readonly MethodInfo Distinct = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Enumerable.Distinct) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

        public static readonly MethodInfo Min = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Enumerable.Min) && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2 && m.GetParameters().Length == 2);

        public static readonly MethodInfo Max = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Enumerable.Max) && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2 && m.GetParameters().Length == 2);
    }
}
