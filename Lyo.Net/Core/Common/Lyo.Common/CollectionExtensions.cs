using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;

namespace Lyo.Common;

/// <summary>Extension methods for collections.</summary>
public static class CollectionExtensions
{
    /// <summary>Resolves <see cref="IReadOnlyList{T}" /> from a sequence, wrapping <see cref="IList{T}" /> with <see cref="ReadOnlyCollection{T}" /> when needed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IReadOnlyList<T> ReadOnlyListOrWrappedOrCopied<T>(IEnumerable<T>? source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        return source as IReadOnlyList<T> ?? (source is IList<T> ilist ? new ReadOnlyCollection<T>(ilist) : source.ToList());
    }

    /// <summary>Resolves <see cref="IReadOnlyCollection{T}" /> from a sequence, wrapping <see cref="IList{T}" /> with <see cref="ReadOnlyCollection{T}" /> when needed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IReadOnlyCollection<T> ReadOnlyCollectionOrWrappedOrCopied<T>(IEnumerable<T>? source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        return source as IReadOnlyCollection<T> ?? (source is IList<T> ilist ? new ReadOnlyCollection<T>(ilist) : source.ToList());
    }

    /// <summary>Reuses materialized sequences when compatible; otherwise allocates a single <see cref="List{T}" />.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TValue ExistingOrMaterializedList<T, TValue>(IEnumerable<T>? source, Func<IEnumerable<T>, TValue?> tryExisting)
        where TValue : class
    {
        ArgumentHelpers.ThrowIfNull(source);
        return tryExisting(source) ?? (TValue)(object)source.ToList();
    }

    /// <summary>Returns the backing array without copying when the sequence is already a <typeparamref name="T" /> array.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] ExistingArrayOrToArray<T>(IEnumerable<T>? source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        return source as T[] ?? source.ToArray();
    }
    // Note: Do not add IsNullOrEmpty on IEnumerable<T> — string implements IEnumerable<char>, so it would
    // win/be ambiguous vs extension(string?).IsNullOrEmpty (semantics differ: empty string vs no chars).
    // Use array IsNullOrEmpty for T[], or inline source == null || !source.Any() for general sequences.
    //extension<T>(IEnumerable<T>? source)
    //{
    //    /// <summary>Returns true if the enumerable is null or contains no elements.</summary>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public bool IsNullOrEmpty() => source == null || !source.Any();
    //}

    extension<T>(T[]? array)
    {
        /// <summary>Returns true if the array is null or has length 0.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullOrEmpty() => array == null || array.Length == 0;
    }

    extension<T>(T[] array)
    {
        /// <summary>Returns the array unchanged.</summary>
        /// <exception cref="ArgumentNullException">Thrown when the array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public T[] AsArrayOrToArray()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }

        /// <summary>Returns the array as <see cref="IReadOnlyList{T}" /> (same instance, no copy).</summary>
        /// <exception cref="ArgumentNullException">Thrown when the array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyList<T> AsReadOnlyList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }

        /// <summary>Returns the array as <see cref="IReadOnlyCollection{T}" /> (same instance, no copy).</summary>
        /// <exception cref="ArgumentNullException">Thrown when the array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyCollection<T> AsReadOnlyCollectionOrToList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }

        /// <summary>Copies elements into a new <see cref="List{T}" />.</summary>
        /// <exception cref="ArgumentNullException">Thrown when the array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public List<T> AsListOrToList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array.ToList();
        }

        /// <summary>Returns the array as <see cref="ICollection{T}" /> (same instance, no copy).</summary>
        /// <exception cref="ArgumentNullException">Thrown when the array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public ICollection<T> AsCollectionOrToList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }
    }

    extension<T>(IEnumerable<T>? source)
    {
        /// <summary>
        /// Returns an <see cref="IReadOnlyList{T}" /> without copying elements when the sequence is already a read-only list, or wraps an <see cref="IList{T}" /> via
        /// <see cref="ReadOnlyCollection{T}" />; otherwise materializes via <see cref="Enumerable.ToList{TSource}" />.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyList<T> AsReadOnlyList() => ReadOnlyListOrWrappedOrCopied(source);

        /// <summary>
        /// Returns an <see cref="IReadOnlyCollection{T}" /> without copying elements when the sequence already exposes one, or wraps an <see cref="IList{T}" /> via
        /// <see cref="ReadOnlyCollection{T}" />; otherwise materializes via <see cref="Enumerable.ToList{TSource}" />.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyCollection<T> AsReadOnlyCollectionOrToList() => ReadOnlyCollectionOrWrappedOrCopied(source);

        /// <summary>Returns the same instance if it is already a <see cref="List{T}" />; otherwise materializes via <see cref="Enumerable.ToList{TSource}" />.</summary>
        /// <remarks>Other <see cref="IList{T}" /> implementations (e.g. arrays) are copied because this method returns a <see cref="List{T}" />.</remarks>
        /// <exception cref="ArgumentNullException">Thrown when the sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public List<T> AsListOrToList() => ExistingOrMaterializedList(source, static s => s as List<T>);

        /// <summary>Returns an <see cref="ICollection{T}" /> without copying when the sequence already exposes one; otherwise materializes via <see cref="Enumerable.ToList{TSource}" />.</summary>
        /// <exception cref="ArgumentNullException">Thrown when the sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public ICollection<T> AsCollectionOrToList() => ExistingOrMaterializedList(source, static s => s as ICollection<T>);

        /// <summary>Returns the same <typeparamref name="T" /> array instance when appropriate; otherwise materializes via <see cref="Enumerable.ToArray{TSource}" />.</summary>
        /// <exception cref="ArgumentNullException">Thrown when the sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public T[] AsArrayOrToArray() => ExistingArrayOrToArray(source);
    }

    extension<TSource>(IEnumerable<TSource> source)
    {
#if NETSTANDARD2_0
        /// <summary>Returns distinct elements by a key selector. Polyfill for .NET Standard 2.0.</summary>
        /// <typeparam name="TKey">The type of key produced by <paramref name="keySelector" />.</typeparam>
        /// <param name="keySelector">A function to extract the comparison key from each element.</param>
        /// <exception cref="ArgumentNullException">Thrown when the sequence or <paramref name="keySelector" /> is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IEnumerable<TSource> DistinctBy<TKey>(Func<TSource, TKey> keySelector)
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
        /// <summary>Returns distinct elements by a key selector.</summary>
        /// <typeparam name="TKey">The type of key produced by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">A function to extract the comparison key from each element.</param>
        public IEnumerable<TSource> DistinctBy<TKey>(Func<TSource, TKey> keySelector) => Enumerable.DistinctBy(source, keySelector);

        /// <inheritdoc cref="Enumerable.DistinctBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey}, IEqualityComparer{TKey}?)"/>
        /// <typeparam name="TKey">The type of key produced by <paramref name="keySelector"/>.</typeparam>
        /// <param name="keySelector">A function to extract the comparison key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        public IEnumerable<TSource> DistinctBy<TKey>(Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => Enumerable.DistinctBy(source, keySelector, comparer);
#endif
    }

#if NETSTANDARD2_0
    // Single receiver type: Dictionary (and others) implement both IDictionary<,> and IReadOnlyDictionary<,> on netstandard2.0,
    // so duplicating the extension for both caused ambiguous invocation.
    extension<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        /// <summary>Gets the value associated with the specified key, or the default value for <typeparamref name="TValue" /> when the key is not found.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue? GetValueOrDefault(TKey key) => source.TryGetValue(key, out var value) ? value : default;

        /// <summary>Gets the value associated with the specified key, or <paramref name="defaultValue" /> when the key is not found.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue? GetValueOrDefault(TKey key, TValue? defaultValue) => source.TryGetValue(key, out var value) ? value : defaultValue;
    }
#endif
}