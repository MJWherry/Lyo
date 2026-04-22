using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Crud.Read;

/// <summary>Builds EF and CLR key expressions for ID-first paging and batch hydration. Shared by QueryService.</summary>
public static class QueryKeyExpressionBuilder
{
    private static readonly ConcurrentDictionary<string, object> EfKeySelectorCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, object> EfKeyInPredicateCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, object> ClrKeyAccessorCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, MethodInfo> EnumerableContainsTwoArg = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression<Func<TDbModel, TKey>> BuildEfKeySelector<TDbModel, TKey>(string keyName)
        where TDbModel : class
    {
        var cacheKey = $"{typeof(TDbModel).FullName}\x1e{typeof(TKey).FullName}\x1e{keyName}";
        return (Expression<Func<TDbModel, TKey>>)EfKeySelectorCache.GetOrAdd(cacheKey, _ => CreateEfKeySelector<TDbModel, TKey>(keyName));
    }

    private static Expression<Func<TDbModel, TKey>> CreateEfKeySelector<TDbModel, TKey>(string keyName)
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
        var cacheKey = BuildEfKeyInPredicateCacheKey<TDbModel, TKey>(keyName, keys);
        return (Expression<Func<TDbModel, bool>>)EfKeyInPredicateCache.GetOrAdd(cacheKey, _ => CreateEfKeyInPredicate<TDbModel, TKey>(keyName, keys));
    }

    private static string BuildEfKeyInPredicateCacheKey<TDbModel, TKey>(string keyName, TKey[] keys)
        where TDbModel : class
    {
        var sb = new StringBuilder();
        sb.Append(typeof(TDbModel).FullName).Append('\x1e').Append(typeof(TKey).FullName).Append('\x1e').Append(keyName).Append('\x1e');
        sb.Append(keys.Length);
        foreach (var k in keys)
            sb.Append('\x1e').Append(k?.ToString() ?? "\0null");

        return sb.ToString();
    }

    private static Expression<Func<TDbModel, bool>> CreateEfKeyInPredicate<TDbModel, TKey>(string keyName, TKey[] keys)
        where TDbModel : class where TKey : notnull
    {
        var parameter = Expression.Parameter(typeof(TDbModel), "e");
        var keyExpr = Expression.Call(typeof(EF), nameof(EF.Property), [typeof(TKey)], parameter, Expression.Constant(keyName));
        var keysConst = Expression.Constant(keys);
        var containsMethod = GetEnumerableContainsTwoArg(typeof(TKey));
        var body = Expression.Call(containsMethod, keysConst, keyExpr);
        return Expression.Lambda<Func<TDbModel, bool>>(body, parameter);
    }

    private static MethodInfo GetEnumerableContainsTwoArg(Type elementType)
        => EnumerableContainsTwoArg.GetOrAdd(
            elementType,
            static et => typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(et));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<TDbModel, TKey> BuildClrKeyAccessor<TDbModel, TKey>(string keyName)
        where TDbModel : class where TKey : notnull
    {
        var cacheKey = $"{typeof(TDbModel).FullName}\x1e{typeof(TKey).FullName}\x1e{keyName}";
        return (Func<TDbModel, TKey>)ClrKeyAccessorCache.GetOrAdd(cacheKey, _ => CreateClrKeyAccessor<TDbModel, TKey>(keyName));
    }

    private static Func<TDbModel, TKey> CreateClrKeyAccessor<TDbModel, TKey>(string keyName)
        where TDbModel : class where TKey : notnull
    {
        var property = typeof(TDbModel).GetProperty(keyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        OperationHelpers.ThrowIfNull(property, $"Primary key property '{keyName}' was not found on '{typeof(TDbModel).Name}'.");
        return entity => (TKey)property.GetValue(entity)!;
    }
}