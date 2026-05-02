using System.Runtime.CompilerServices;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Extension methods for collections.</summary>
public static class CollectionExtensions
{
    /// <summary>Returns true if the enumerable is null or contains no elements.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => source == null || !source.Any();

    /// <summary>Returns the enumerable as a read-only list, avoiding multiple enumeration.</summary>
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        return source as IReadOnlyList<T> ?? source.ToList();
    }

    /// <summary>Returns distinct elements by a key selector. Polyfill for .NET Standard 2.0.</summary>
#if NETSTANDARD2_0
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(keySelector);
        var seen = new HashSet<TKey>();
        foreach (var element in source) {
            if (seen.Add(keySelector(element)))
                yield return element;
        }
    }
#else
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        => Enumerable.DistinctBy(source, keySelector);

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer)
        => Enumerable.DistinctBy(source, keySelector, comparer);
#endif
}