using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Services.Aggregate;

/// <summary>One page of aggregate rows. <see cref="Total" /> is the number of groups, except an ungrouped summary, which is one row.</summary>
public sealed class AggregateQueryPage(IReadOnlyList<Dictionary<string, object?>> items, int? total, bool? hasMore)
{
    public IReadOnlyList<Dictionary<string, object?>> Items { get; } = items;

    public int? Total { get; } = total;

    public bool? HasMore { get; } = hasMore;
}

/// <summary>Translatable grouped query plus the shaper that turns each row into a dictionary.</summary>
public sealed class AggregateQueryPlan
{
    private readonly AggregateRowShape _shape;
    private readonly string[] _keyNames;
    private readonly string[] _aggregateNames;
    private readonly KeyValuePair<string, object?>[] _emptyAggregates;

    internal AggregateQueryPlan(IQueryable ordered, AggregateRowShape shape, string[] keyNames, string[] aggregateNames, KeyValuePair<string, object?>[] emptyAggregates)
    {
        Ordered = ordered;
        _shape = shape;
        _keyNames = keyNames;
        _aggregateNames = aggregateNames;
        _emptyAggregates = emptyAggregates;
    }

    /// <summary>Grouped, ordered query before paging. The element type is <see cref="RowType" />.</summary>
    public IQueryable Ordered { get; }

    /// <summary>CLR type of <see cref="Ordered" />. Providers materialize this type, then <see cref="ToRows" /> reads it.</summary>
    public Type RowType => _shape.Type;

    /// <summary>True when <c>Select</c> is empty and the result is one summary row.</summary>
    public bool Ungrouped => _keyNames.Length == 0;

    /// <summary>Skip <paramref name="start" /> groups and take <paramref name="take" />. <see cref="int.MaxValue" /> takes the rest.</summary>
    public IQueryable Slice(int start, int take)
    {
        IQueryable query = Ordered;
        if (start > 0)
            query = Apply("Skip", query, start);

        if (take < int.MaxValue)
            query = Apply("Take", query, take);

        return query;
    }

    /// <summary>Maps materialized rows to dictionaries keyed by the request's select paths and aggregate names.</summary>
    public IReadOnlyList<Dictionary<string, object?>> ToRows(IEnumerable materialized)
    {
        var rows = new List<Dictionary<string, object?>>();
        foreach (var item in materialized) {
            if (item is null)
                continue;

            var dict = new Dictionary<string, object?>(_keyNames.Length + _aggregateNames.Length, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _keyNames.Length; i++)
                dict[_keyNames[i]] = _shape.Properties[i].GetValue(item);

            for (var i = 0; i < _aggregateNames.Length; i++)
                dict[_aggregateNames[i]] = _shape.Properties[_keyNames.Length + i].GetValue(item);

            rows.Add(dict);
        }

        return rows;
    }

    /// <summary>Summary row used when an ungrouped query matches nothing. <c>Count</c> is 0, a non-nullable sum is 0, and the other aggregates are null.</summary>
    public Dictionary<string, object?> EmptySummary()
    {
        var dict = new Dictionary<string, object?>(_emptyAggregates.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _emptyAggregates)
            dict[pair.Key] = pair.Value;

        return dict;
    }

    internal AggregateQueryPage Execute(int start, int amount, QueryTotalCountMode mode)
    {
        int? groupCount = mode == QueryTotalCountMode.Exact || Ungrouped ? Count(Ordered) : null;
        if (Ungrouped && groupCount == 0) {
            var items = start == 0 && amount > 0 ? (IReadOnlyList<Dictionary<string, object?>>)[EmptySummary()] : [];
            int? total = mode == QueryTotalCountMode.None ? null : 1;
            bool? hasMore = mode == QueryTotalCountMode.HasMore ? false : null;
            return new(items, total, hasMore);
        }

        var take = amount;
        var extra = false;
        if (mode == QueryTotalCountMode.HasMore && amount < int.MaxValue) {
            take = amount + 1;
            extra = true;
        }

        var fetched = ToRows(Materialize(Slice(start, take)));
        bool? more = null;
        if (extra) {
            more = fetched.Count > amount;
            if (more == true)
                fetched = fetched.Take(amount).ToList();
        }

        int? totalOut = mode switch {
            QueryTotalCountMode.Exact when Ungrouped => 1,
            QueryTotalCountMode.Exact => groupCount,
            QueryTotalCountMode.HasMore when Ungrouped => 1,
            QueryTotalCountMode.HasMore when more == false => start + fetched.Count,
            _ => null
        };

        return new(fetched, totalOut, more);
    }

    private IQueryable Apply(string name, IQueryable query, int value)
    {
        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == name && m.IsGenericMethodDefinition && m.GetParameters() is [_, { ParameterType: var second }] && second == typeof(int))
            .MakeGenericMethod(RowType);

        return (IQueryable)Invoke(method, [query, value]);
    }

    private static int Count(IQueryable query)
    {
        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Queryable.Count) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
            .MakeGenericMethod(query.ElementType);

        return (int)Invoke(method, [query]);
    }

    private static IEnumerable Materialize(IQueryable query)
    {
        var method = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(query.ElementType);
        return (IEnumerable)Invoke(method, [query]);
    }

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
}
