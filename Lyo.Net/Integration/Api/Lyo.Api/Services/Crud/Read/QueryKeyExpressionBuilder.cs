using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Read;

/// <summary>Builds EF and CLR key expressions for ID-first paging and batch hydration. Shared by QueryService.</summary>
public static class QueryKeyExpressionBuilder
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression<Func<TDbModel, TKey>> BuildEfKeySelector<TDbModel, TKey>(string keyName)
        where TDbModel : class
    {
        var parameter = Expression.Parameter(typeof(TDbModel), "e");
        var body = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(TKey)], parameter, Expression.Constant(keyName));
        return Expression.Lambda<Func<TDbModel, TKey>>(body, parameter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression<Func<TDbModel, bool>> BuildEfKeyInPredicate<TDbModel, TKey>(string keyName, TKey[] keys)
        where TDbModel : class where TKey : notnull
    {
        var parameter = Expression.Parameter(typeof(TDbModel), "e");
        var keyExpr = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(TKey)], parameter, Expression.Constant(keyName));
        var keysConst = Expression.Constant(keys);
        var containsMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TKey));

        var body = Expression.Call(containsMethod, keysConst, keyExpr);
        return Expression.Lambda<Func<TDbModel, bool>>(body, parameter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<TDbModel, TKey> BuildClrKeyAccessor<TDbModel, TKey>(string keyName)
        where TDbModel : class where TKey : notnull
    {
        var property = typeof(TDbModel).GetProperty(keyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        OperationHelpers.ThrowIfNull(property, $"Primary key property '{keyName}' was not found on '{typeof(TDbModel).Name}'.");
        return entity => (TKey)property.GetValue(entity)!;
    }
}