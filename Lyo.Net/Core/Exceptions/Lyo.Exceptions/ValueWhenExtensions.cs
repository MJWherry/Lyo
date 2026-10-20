using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

/// <summary>
/// Fluent null coalescing and conditional value picks. Same-type <c>OrIfNull</c> returns the receiver's type.
/// Checks that return a different type return <see cref="ValueOrChain{TSubject,TResult}" />.
/// </summary>
/// <remarks>
/// Ordering uses <see cref="IComparable{T}" />, so numbers, <see cref="string" />, <see cref="DateTime" />, <see cref="DateTimeOffset" />, <see cref="TimeSpan" />,
/// <c>DateOnly</c>, and <c>TimeOnly</c> share one range implementation. Empty and whitespace strings stay present for <c>OrIfNull</c>;
/// use <see cref="StringOrExtensions" /> when empty or whitespace should count as missing.
/// </remarks>
public static partial class ValueWhenExtensions
{
    extension<T>(T? value) where T : class
    {
        /// <summary>Returns <paramref name="value" /> when it is not null; otherwise <paramref name="alternative" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(alternative))]
        public T? OrIfNull(T? alternative) => value ?? alternative;

        /// <summary>Returns <paramref name="value" /> when it is not null. Invokes <paramref name="alternative" /> only when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? OrIfNull(Func<T?> alternative)
        {
            if (value is not null)
                return value;

            ArgumentHelpers.ThrowIfNull(alternative);
            return alternative();
        }

        /// <summary>Returns <paramref name="value" /> when it is not null; otherwise <paramref name="defaultValue" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T OrDefault(T defaultValue) => value ?? defaultValue;

        /// <summary>Returns <paramref name="value" /> when it is not null. Invokes <paramref name="defaultValue" /> only when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T OrDefault(Func<T> defaultValue)
        {
            if (value is not null)
                return value;

            ArgumentHelpers.ThrowIfNull(defaultValue);
            return defaultValue();
        }
    }

    extension<T>(T? value) where T : struct
    {
        /// <summary>Returns <paramref name="value" /> when it has a value; otherwise <paramref name="alternative" />. Zero is present.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? OrIfNull(T? alternative) => value ?? alternative;

        /// <summary>Returns <paramref name="value" /> when it has a value. Invokes <paramref name="alternative" /> only when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? OrIfNull(Func<T?> alternative)
        {
            if (value.HasValue)
                return value;

            ArgumentHelpers.ThrowIfNull(alternative);
            return alternative();
        }

        /// <summary>Returns the contained value, or <paramref name="defaultValue" /> when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T OrDefault(T defaultValue) => value ?? defaultValue;

        /// <summary>Returns the contained value. Invokes <paramref name="defaultValue" /> only when <paramref name="value" /> is null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T OrDefault(Func<T> defaultValue)
        {
            if (value.HasValue)
                return value.GetValueOrDefault();

            ArgumentHelpers.ThrowIfNull(defaultValue);
            return defaultValue();
        }
    }

    extension<TSubject>(TSubject value)
    {
        /// <summary>Chooses <paramref name="result" /> when <paramref name="predicate" /> is true for <paramref name="value" />; otherwise leaves the chain open.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject, TResult> If<TResult>(Func<TSubject, bool> predicate, TResult result)
        {
            ArgumentHelpers.ThrowIfNull(predicate);
            return ValueOrChain<TSubject, TResult>.From(value, predicate(value), result);
        }

        /// <summary>Chooses the factory result when <paramref name="predicate" /> is true. The factory runs only when the check passes.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject, TResult> If<TResult>(Func<TSubject, bool> predicate, Func<TResult> result)
        {
            ArgumentHelpers.ThrowIfNull(predicate);
            return ValueOrChain<TSubject, TResult>.From(value, predicate(value), result);
        }
    }

    extension<TSubject, TResult>(ValueOrChain<TSubject, TResult> chain)
    {
        /// <summary>When the chain is still open and <paramref name="condition" /> is true, chooses <paramref name="result" />. A chosen chain is left unchanged.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject, TResult> OrWhen(bool condition, TResult result) => chain.IsChosen ? chain : chain.Choose(condition, result);

        /// <summary>
        /// When the chain is still open and <paramref name="condition" /> is true, invokes <paramref name="result" />.
        /// A chosen chain does not invoke the factory.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject, TResult> OrWhen(bool condition, Func<TResult> result) => chain.IsChosen ? chain : chain.Choose(condition, result);

        /// <summary>When the chain is still open and <paramref name="predicate" /> is true for the subject, chooses <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject, TResult> OrWhen(Func<TSubject, bool> predicate, TResult result)
        {
            if (chain.IsChosen)
                return chain;

            ArgumentHelpers.ThrowIfNull(predicate);
            return chain.Choose(predicate(chain.Subject), result);
        }

        /// <summary>When the chain is still open and <paramref name="predicate" /> is true for the subject, invokes <paramref name="result" />.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueOrChain<TSubject, TResult> OrWhen(Func<TSubject, bool> predicate, Func<TResult> result)
        {
            if (chain.IsChosen)
                return chain;

            ArgumentHelpers.ThrowIfNull(predicate);
            return chain.Choose(predicate(chain.Subject), result);
        }
    }
}
