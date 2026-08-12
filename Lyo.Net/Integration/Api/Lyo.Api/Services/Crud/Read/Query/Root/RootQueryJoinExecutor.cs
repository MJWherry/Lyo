using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Read.Query.Root;

/// <summary>
/// Executes root From/Joins as EF-translatable joins (arbitrary ON columns, chained aliases). Left: <c>SelectMany(o =&gt; inner.Where(on).DefaultIfEmpty(), …)</c> — not
/// GroupJoin+ValueTuple (untranslatable). Final Select is sparse only.
/// </summary>
internal static class RootQueryJoinExecutor
{
    public static Task<List<object?[]>> ExecuteAsync(
        IQueryable fromSet,
        Type fromClr,
        int start,
        int amount,
        IReadOnlyList<IQueryable> scopedJoinSets,
        RootQueryShapePlan plan,
        CancellationToken ct)
    {
        if (scopedJoinSets.Count != plan.Joins.Count)
            throw new ArgumentException("scopedJoinSets count must match plan.Joins.");

        // +1 slot for From PK used to collapse join fan-out (ValueTuple max 7 without Rest nesting).
        if (plan.SelectSpecs.Count is < 1 or > 6)
            throw new InvalidQueryException("Root /Query Select must have 1–6 fields in v1.");

        if (plan.Joins.Count > 7)
            throw new InvalidQueryException("Root /Query supports at most 7 joins in v1.");

        var method = typeof(RootQueryJoinExecutor).GetMethod(nameof(ExecuteCore), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(fromClr);
        return (Task<List<object?[]>>)method.Invoke(null, [fromSet, start, amount, scopedJoinSets, plan, ct])!;
    }

    private static async Task<List<object?[]>> ExecuteCore<TFrom>(
        IQueryable fromSet,
        int start,
        int amount,
        IReadOnlyList<IQueryable> scopedJoinSets,
        RootQueryShapePlan plan,
        CancellationToken ct)
        where TFrom : class
    {
        var carrier = ApplySkipTake((IQueryable<TFrom>)fromSet, start, amount);
        var carrierType = typeof(TFrom);
        var aliasAccess = new Dictionary<string, Func<Expression, Expression>>(StringComparer.OrdinalIgnoreCase) { [plan.FromAlias] = e => e };
        for (var ji = 0; ji < plan.Joins.Count; ji++) {
            var joinPlan = plan.Joins[ji];
            var on = joinPlan.On[0];
            var joinClr = scopedJoinSets[ji].ElementType;
            var joinSet = scopedJoinSets[ji];
            if (!aliasAccess.TryGetValue(on.LeftAlias, out var leftEntityAccess))
                throw new InvalidQueryException($"Join ON left alias '{on.LeftAlias}' is unknown.");

            var pairType = typeof(RootJoinPair<,>).MakeGenericType(carrierType, joinClr);
            var outerP = Expression.Parameter(carrierType, "outer");
            var leftEntity = leftEntityAccess(outerP);
            var innerP = Expression.Parameter(joinClr, "inner");

            // ON: left.Prop == right.Prop (same CLR types — no Guid? casts; EF rejects those in GroupJoin)
            Expression leftKey = Expression.Property(leftEntity, on.LeftProperty);
            Expression rightKey = Expression.Property(innerP, on.RightProperty);
            if (leftKey.Type != rightKey.Type) {
                // Only convert when one side is nullable of the other
                if (Nullable.GetUnderlyingType(leftKey.Type) == rightKey.Type)
                    rightKey = Expression.Convert(rightKey, leftKey.Type);
                else if (Nullable.GetUnderlyingType(rightKey.Type) == leftKey.Type)
                    leftKey = Expression.Convert(leftKey, rightKey.Type);
                else
                    throw new InvalidQueryException($"Join ON key type mismatch: {leftKey.Type} vs {rightKey.Type}.");
            }

            Expression onEqual = Expression.Equal(leftKey, rightKey);
            // When left entity can be null (prior left join), short-circuit: no match if left is null
            if (!leftEntity.Type.IsValueType)
                onEqual = Expression.AndAlso(Expression.NotEqual(leftEntity, Expression.Constant(null, leftEntity.Type)), onEqual);

            var whereCall = Expression.Call(typeof(Queryable), nameof(Queryable.Where), [joinClr], joinSet.Expression, Expression.Quote(Expression.Lambda(onEqual, innerP)));
            Expression collection;
            if (joinPlan.Type == JoinType.Left)
                collection = Expression.Call(typeof(Queryable), nameof(Queryable.DefaultIfEmpty), [joinClr], whereCall);
            else
                collection = whereCall;

            var collectionSel = Expression.Lambda(typeof(Func<,>).MakeGenericType(carrierType, typeof(IEnumerable<>).MakeGenericType(joinClr)), collection, outerP);
            var resultOuterP = Expression.Parameter(carrierType, "o");
            var resultInnerP = Expression.Parameter(joinClr, "j");
            var pairNew = Expression.MemberInit(
                Expression.New(pairType), Expression.Bind(pairType.GetProperty(nameof(RootJoinPair<object, object>.Outer))!, resultOuterP),
                Expression.Bind(pairType.GetProperty(nameof(RootJoinPair<object, object>.Inner))!, resultInnerP));

            var resultSel = Expression.Lambda(typeof(Func<,,>).MakeGenericType(carrierType, joinClr, pairType), pairNew, resultOuterP, resultInnerP);
            carrier = carrier.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), nameof(Queryable.SelectMany), [carrierType, joinClr, pairType], carrier.Expression, Expression.Quote(collectionSel),
                    Expression.Quote(resultSel)));

            carrierType = pairType;
            var prevAccess = new Dictionary<string, Func<Expression, Expression>>(aliasAccess, StringComparer.OrdinalIgnoreCase);
            aliasAccess.Clear();
            foreach (var (alias, access) in prevAccess)
                aliasAccess[alias] = CaptureOuter(access, pairType);

            aliasAccess[joinPlan.Alias] = e => Expression.Property(e, nameof(RootJoinPair<object, object>.Inner));
        }

        // Row = [FromPk, ...SelectSpecs] so we can collapse join fan-out to one item per From.
        var selectTypes = new Type[1 + plan.SelectSpecs.Count];
        selectTypes[0] = plan.FromPrimaryKey.PropertyType;
        for (var s = 0; s < plan.SelectSpecs.Count; s++) {
            var t = plan.SelectSpecs[s].Property.PropertyType;
            if (!plan.SelectSpecs[s].IsFromSide && t.IsValueType && Nullable.GetUnderlyingType(t) is null)
                t = typeof(Nullable<>).MakeGenericType(t);

            selectTypes[s + 1] = t;
        }

        var rowType = MakeValueTupleType(selectTypes);
        var rowP = Expression.Parameter(carrierType, "row");
        var elems = new Expression[selectTypes.Length];
        var fromEntity = aliasAccess[plan.FromAlias](rowP);
        elems[0] = AlignType(Expression.Property(fromEntity, plan.FromPrimaryKey), selectTypes[0]);
        for (var s = 0; s < plan.SelectSpecs.Count; s++) {
            var spec = plan.SelectSpecs[s];
            if (!aliasAccess.TryGetValue(spec.Alias, out var entityAccess))
                throw new InvalidQueryException($"Select alias '{spec.Alias}' unknown.");

            var entity = entityAccess(rowP);
            Expression value = Expression.Property(entity, spec.Property);
            if (!spec.IsFromSide && !entity.Type.IsValueType) {
                value = Expression.Condition(
                    Expression.Equal(entity, Expression.Constant(null, entity.Type)), Expression.Default(selectTypes[s + 1]), AlignType(value, selectTypes[s + 1]));
            }
            else
                value = AlignType(value, selectTypes[s + 1]);

            elems[s + 1] = value;
        }

        var projected = carrier.Provider.CreateQuery(
            Expression.Call(
                typeof(Queryable), nameof(Queryable.Select), [carrierType, rowType], carrier.Expression,
                Expression.Quote(Expression.Lambda(typeof(Func<,>).MakeGenericType(carrierType, rowType), Expression.New(rowType.GetConstructor(selectTypes)!, elems), rowP))));

        var list = await ToListAsync(projected, rowType, ct).ConfigureAwait(false);
        var fields = Enumerable.Range(0, selectTypes.Length).Select(i => rowType.GetField("Item" + (i + 1))!).ToArray();
        var result = new List<object?[]>(list.Count);
        foreach (var row in list) {
            var values = new object?[fields.Length];
            for (var i = 0; i < fields.Length; i++)
                values[i] = fields[i].GetValue(row);

            result.Add(values);
        }

        return result;
    }

    private static Func<Expression, Expression> CaptureOuter(Func<Expression, Expression> prevAccess, Type pairType)
        => carrier => prevAccess(Expression.Property(carrier, nameof(RootJoinPair<object, object>.Outer)));

    private static IQueryable ApplySkipTake(IQueryable source, int start, int amount)
    {
        var t = source.ElementType;
        var skipped = source.Provider.CreateQuery(Expression.Call(typeof(Queryable), nameof(Queryable.Skip), [t], source.Expression, Expression.Constant(start)));
        return skipped.Provider.CreateQuery(Expression.Call(typeof(Queryable), nameof(Queryable.Take), [t], skipped.Expression, Expression.Constant(amount)));
    }

    private static async Task<IList> ToListAsync(IQueryable query, Type elementType, CancellationToken ct)
    {
        var method = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        var task = (Task)method.Invoke(null, [query, ct])!;
        await task.ConfigureAwait(false);
        return (IList)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static Type MakeValueTupleType(Type[] args)
        => args.Length switch {
            1 => typeof(ValueTuple<>).MakeGenericType(args),
            2 => typeof(ValueTuple<,>).MakeGenericType(args),
            3 => typeof(ValueTuple<,,>).MakeGenericType(args),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(args),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(args),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(args),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(args),
            8 => typeof(ValueTuple<,,,,,,,>).MakeGenericType(args),
            var _ => throw new InvalidQueryException("Unsupported select arity.")
        };

    private static Expression AlignType(Expression expr, Type targetType) => expr.Type == targetType ? expr : Expression.Convert(expr, targetType);
}

/// <summary>EF-translatable join carrier (class + properties; ValueTuple GroupJoin is not translated).</summary>
internal sealed class RootJoinPair<TOuter, TInner>
{
    public TOuter Outer { get; set; } = default!;

    public TInner? Inner { get; set; }
}

internal sealed record RootQueryShapePlan(
    string FromAlias,
    PropertyInfo FromPrimaryKey,
    IReadOnlyList<RootQuerySelectSpec> SelectSpecs,
    IReadOnlyList<RootQueryJoinPlan> Joins,
    IReadOnlyList<string> EntityTypeNames);

internal sealed record RootQuerySelectSpec(string RequestedPath, string Alias, string PropertyName, PropertyInfo Property, bool IsFromSide, string? JoinResultName);

internal sealed record RootQueryJoinPlan(string Alias, string EntityTypeName, string ResultName, JoinType Type, IReadOnlyList<RootQueryOnPlan> On, SourceQueryScope? SourceQuery);

internal sealed record RootQueryOnPlan(string LeftAlias, PropertyInfo LeftProperty, string RightAlias, PropertyInfo RightProperty);