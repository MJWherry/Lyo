using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;

namespace Lyo.Common.Core;

/// <summary>Helpers that reuse or materialize sequences as lists, arrays, and read-only views.</summary>
public static class CollectionExtensions
{
    /// <summary>Turns a sequence into <see cref="IReadOnlyList{T}" />, wrapping <see cref="IList{T}" /> with <see cref="ReadOnlyCollection{T}" /> when needed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IReadOnlyList<T> ReadOnlyListOrWrappedOrCopied<T>(IEnumerable<T>? source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        return source as IReadOnlyList<T> ?? (source is IList<T> ilist ? new ReadOnlyCollection<T>(ilist) : source.ToList());
    }

    /// <summary>Turns a sequence into <see cref="IReadOnlyCollection{T}" />, wrapping <see cref="IList{T}" /> with <see cref="ReadOnlyCollection{T}" /> when needed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IReadOnlyCollection<T> ReadOnlyCollectionOrWrappedOrCopied<T>(IEnumerable<T>? source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        return source as IReadOnlyCollection<T> ?? (source is IList<T> iList ? new ReadOnlyCollection<T>(iList) : source.ToList());
    }

    /// <summary>Reuses a compatible materialized sequence; otherwise allocates one <see cref="List{T}" />.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TValue ExistingOrMaterializedList<T, TValue>(IEnumerable<T>? source, Func<IEnumerable<T>, TValue?> tryExisting)
        where TValue : class
    {
        ArgumentHelpers.ThrowIfNull(source);
        // ReSharper disable once PossibleMultipleEnumeration - cast only; does not walk the sequence
        return tryExisting(source)
            // ReSharper disable once PossibleMultipleEnumeration
            ?? (TValue)(object)source.ToList();
    }

    /// <summary>Returns the backing array with no copy when the sequence is already a <typeparamref name="T" /> array.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] ExistingArrayOrToArray<T>(IEnumerable<T>? source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        return source as T[] ?? source.ToArray();
    }
    // Do not add IsNullOrEmpty on IEnumerable<T> — string implements IEnumerable<char>, so it would
    // win or be ambiguous vs extension(string?).IsNullOrEmpty (empty string vs no chars).
    // Use array IsNullOrEmpty for T[], or inline source == null || !source.Any() for general sequences.
    //extension<T>(IEnumerable<T>? source)
    //{
    //    /// <summary>True when the enumerable is null or has no elements.</summary>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public bool IsNullOrEmpty() => source == null || !source.Any();
    //}

    extension<T>(T[]? array)
    {
        /// <summary>True when the array is null or has length 0.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullOrEmpty() => array == null || array.Length == 0;
    }

    extension<T>(T[] array)
    {
        /// <summary>Returns the array unchanged.</summary>
        /// <exception cref="ArgumentNullException">The array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public T[] AsArrayOrToArray()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }

        /// <summary>Returns the array as <see cref="IReadOnlyList{T}" /> (same instance, no copy).</summary>
        /// <exception cref="ArgumentNullException">The array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyList<T> AsReadOnlyList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }

        /// <summary>Returns the array as <see cref="IReadOnlyCollection{T}" /> (same instance, no copy).</summary>
        /// <exception cref="ArgumentNullException">The array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyCollection<T> AsReadOnlyCollectionOrToList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }

        /// <summary>Copies elements into a new <see cref="List{T}" />.</summary>
        /// <exception cref="ArgumentNullException">The array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public List<T> AsListOrToList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array.ToList();
        }

        /// <summary>Returns the array as <see cref="ICollection{T}" /> (same instance, no copy).</summary>
        /// <exception cref="ArgumentNullException">The array is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public ICollection<T> AsCollectionOrToList()
        {
            ArgumentHelpers.ThrowIfNull(array);
            return array;
        }
    }

    extension<T>(IEnumerable<T>? source)
    {
        /// <summary>
        /// Returns an <see cref="IReadOnlyList{T}" /> without copying when the sequence is already a read-only list, or wraps an <see cref="IList{T}" /> via
        /// <see cref="ReadOnlyCollection{T}" />; otherwise materializes via <see cref="Enumerable.ToList{TSource}" />.
        /// </summary>
        /// <exception cref="ArgumentNullException">The sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyList<T> AsReadOnlyList() => ReadOnlyListOrWrappedOrCopied(source);

        /// <summary>
        /// Returns an <see cref="IReadOnlyCollection{T}" /> without copying when the sequence already exposes one, or wraps an <see cref="IList{T}" /> via
        /// <see cref="ReadOnlyCollection{T}" />; otherwise materializes via <see cref="Enumerable.ToList{TSource}" />.
        /// </summary>
        /// <exception cref="ArgumentNullException">The sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public IReadOnlyCollection<T> AsReadOnlyCollectionOrToList() => ReadOnlyCollectionOrWrappedOrCopied(source);

        /// <summary>Returns the same instance when it is already a <see cref="List{T}" />; otherwise materializes via <see cref="Enumerable.ToList{TSource}" />.</summary>
        /// <remarks>Other <see cref="IList{T}" /> implementations (for example arrays) are copied because this method returns a <see cref="List{T}" />.</remarks>
        /// <exception cref="ArgumentNullException">The sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public List<T> AsListOrToList() => ExistingOrMaterializedList(source, static s => s as List<T>);

        /// <summary>
        /// Returns an <see cref="ICollection{T}" /> without copying when the sequence already exposes one; otherwise materializes via
        /// <see cref="Enumerable.ToList{TSource}" />.
        /// </summary>
        /// <exception cref="ArgumentNullException">The sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public ICollection<T> AsCollectionOrToList() => ExistingOrMaterializedList(source, static s => s as ICollection<T>);

        /// <summary>Returns the same <typeparamref name="T" /> array instance when appropriate; otherwise materializes via <see cref="Enumerable.ToArray{TSource}" />.</summary>
        /// <exception cref="ArgumentNullException">The sequence is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
        public T[] AsArrayOrToArray() => ExistingArrayOrToArray(source);
    }

    extension<TSource>(IEnumerable<TSource> source)
    {
#if NETSTANDARD2_0
        /// <summary>Distinct elements by a key selector. Polyfill for .NET Standard 2.0.</summary>
        /// <typeparam name="TKey">Key type produced by <paramref name="keySelector" />.</typeparam>
        /// <param name="keySelector">Extracts the comparison key from each element.</param>
        /// <exception cref="ArgumentNullException">The sequence or <paramref name="keySelector" /> is null; delegates to <see cref="ArgumentHelpers.ThrowIfNull" />.</exception>
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
        /// <summary>Distinct elements by a key selector.</summary>
        /// <typeparam name="TKey">Key type produced by <paramref name="keySelector" />.</typeparam>
        /// <param name="keySelector">Extracts the comparison key from each element.</param>
        public IEnumerable<TSource> DistinctBy<TKey>(Func<TSource, TKey> keySelector) => Enumerable.DistinctBy(source, keySelector);

        /// <inheritdoc cref="Enumerable.DistinctBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey}, IEqualityComparer{TKey}?)" />
        /// <typeparam name="TKey">Key type produced by <paramref name="keySelector" />.</typeparam>
        /// <param name="keySelector">Extracts the comparison key from each element.</param>
        /// <param name="comparer"><see cref="IEqualityComparer{T}" /> used to compare keys.</param>
        public IEnumerable<TSource> DistinctBy<TKey>(Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) => Enumerable.DistinctBy(source, keySelector, comparer);
#endif
    }

#if NETSTANDARD2_0
    // One receiver type: Dictionary (and others) implement both IDictionary<,> and IReadOnlyDictionary<,> on netstandard2.0,
    // so duplicating the extension for both made the call ambiguous.
    extension<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        /// <summary>Value for the key, or the default for <typeparamref name="TValue" /> when the key is missing.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue? GetValueOrDefault(TKey key) => source.TryGetValue(key, out var value) ? value : default;

        /// <summary>Value for the key, or <paramref name="defaultValue" /> when the key is missing.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue? GetValueOrDefault(TKey key, TValue? defaultValue) => source.TryGetValue(key, out var value) ? value : defaultValue;
    }
#endif
}