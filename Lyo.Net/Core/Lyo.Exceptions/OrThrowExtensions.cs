using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

/// <summary>
/// Throws when a candidate value is logically missing (for strings: the same situations as <see cref="string.IsNullOrEmpty(string?)"/> /
/// <see cref="string.IsNullOrWhiteSpace(string?)"/>, matching typical <c>OrDefault</c> behavior on optional configuration strings).
/// </summary>
/// <remarks>
/// <para>
/// C# cannot instantiate <c>new TException(message)</c> from a generic type parameter without reflection. Use overloads taking
/// <see cref="Func{TResult}" />/<see cref="Func{T,TResult}" /> to build arbitrary exception types—no reflection. Message parameters marked optional omit to the defaults
/// defined on each member.
/// </para>
/// </remarks>
public static class OrThrowExtensions
{
    private const string DefaultRequiredStringMissing = "The string value cannot be null or empty.";
    private const string DefaultRequiredStringWhitespaceMissing = "The string value cannot be null, empty, or whitespace.";
    private const string DefaultGenericFactoryMissing = DefaultRequiredStringMissing;
    private const string DefaultRequiredReferenceMissing = "A required value was null.";
    private const string DefaultNullableStructMissing = "A required non-null value was missing.";
    private const string DefaultInvalidOperationMissingStringWhitespace = DefaultRequiredStringWhitespaceMissing;
    private const string DefaultArgumentMissingString = "The argument value cannot be null or empty.";
    private const string DefaultKeyNotFoundMissingString = "The requested configuration or lookup value was not found.";
    private const string DefaultNotSupportedMissingString = "The requested value or scenario is not supported.";

    /// <param name="value">The candidate string.</param>
    extension(string? value)
    {
        /// <summary>Returns the string when it is not null and not empty; otherwise throws.</summary>
        /// <param name="createException">Builds the exception to throw.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrow(Func<Exception> createException)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return s;

            ArgumentHelpers.ThrowIfNull(createException);
            throw createException();
        }

        /// <summary>Returns the string when it is not null, empty, or whitespace; otherwise throws.</summary>
        /// <param name="createException">Builds the exception to throw.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrowIfWhiteSpace(Func<Exception> createException)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return s;

            ArgumentHelpers.ThrowIfNull(createException);
            throw createException();
        }

        /// <summary>Returns this string when not null or empty; otherwise invokes <paramref name="createException" /> with message <paramref name="message" /> (or a default when null or omitted).</summary>
        /// <param name="createException"><c>(message)=&gt;new SomeException(message)</c></param>
        /// <param name="message">Passed to <paramref name="createException" /> when missing.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrow(Func<string, Exception> createException, string? message = null)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return s;

            ArgumentHelpers.ThrowIfNull(createException);
            throw createException(message ?? DefaultGenericFactoryMissing);
        }

        /// <summary>Returns this string when not null, empty, or whitespace; otherwise invokes <paramref name="createException" /> with message <paramref name="message" /> (or a default when null or omitted).</summary>
        /// <inheritdoc cref="OrThrow(System.Func{string,System.Exception},System.String)" path="/param[@name='createException']"/>
        /// <inheritdoc cref="OrThrow(System.Func{string,System.Exception},System.String)" path="/param[@name='message']"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrowIfWhiteSpace(Func<string, Exception> createException, string? message = null)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return s;

            ArgumentHelpers.ThrowIfNull(createException);
            throw createException(message ?? DefaultGenericFactoryMissing);
        }

        /// <summary>Returns the string when it is not null and not empty; otherwise throws <see cref="InvalidOperationException" />.</summary>
        /// <param name="message">Exception message when missing; omit or pass null for a built-in default.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrowInvalidOperation(string? message = null)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return s;

            throw new InvalidOperationException(message ?? DefaultRequiredStringMissing);
        }

        /// <summary>Returns the string when it is not null, empty, or whitespace; otherwise throws <see cref="InvalidOperationException" />.</summary>
        /// <inheritdoc cref="OrThrowInvalidOperation(string?)" path="/param[@name='message']"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrowInvalidOperationIfWhiteSpace(string? message = null)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return s;

            throw new InvalidOperationException(message ?? DefaultInvalidOperationMissingStringWhitespace);
        }

        /// <summary>Returns the string when it is not null and not empty; otherwise throws <see cref="ArgumentException" />.</summary>
        /// <inheritdoc cref="OrThrowInvalidOperation(string?)" path="/param[@name='message']"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrowArgument(string? message = null)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return s;

            throw new ArgumentException(message ?? DefaultArgumentMissingString);
        }

        /// <summary>Returns the string when it is not null and not empty; otherwise throws <see cref="KeyNotFoundException" />.</summary>
        /// <inheritdoc cref="OrThrowInvalidOperation(string?)" path="/param[@name='message']"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrowKeyNotFound(string? message = null)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return s;

            throw new KeyNotFoundException(message ?? DefaultKeyNotFoundMissingString);
        }

        /// <summary>Returns the string when it is not null and not empty; otherwise throws <see cref="NotSupportedException" />.</summary>
        /// <inheritdoc cref="OrThrowInvalidOperation(string?)" path="/param[@name='message']"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrThrowNotSupported(string? message = null)
        {
            if (value is string s && !string.IsNullOrEmpty(s))
                return s;

            throw new NotSupportedException(message ?? DefaultNotSupportedMissingString);
        }
    }

    /// <summary>Returns <paramref name="value" /> when not null; otherwise throws <see cref="InvalidOperationException" />.</summary>
    /// <param name="value">Reference-type candidate.</param>
    /// <param name="message">Exception message; omit or pass null for a built-in default.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T OrThrowInvalidOperation<T>(this T? value, string? message = null)
        where T : class
        => value ?? throw new InvalidOperationException(message ?? DefaultRequiredReferenceMissing);

    /// <summary>Returns the contained value when <paramref name="value" /> has a value; otherwise throws <see cref="InvalidOperationException" />.</summary>
    /// <param name="value">Nullable value-type candidate.</param>
    /// <param name="message">Exception message; omit or pass null for a built-in default.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T OrThrowInvalidOperation<T>(this T? value, string? message = null)
        where T : struct
    {
        if (value.HasValue)
            return value.GetValueOrDefault();

        throw new InvalidOperationException(message ?? DefaultNullableStructMissing);
    }

    /// <summary>Returns <paramref name="value" /> when not null; otherwise invokes <paramref name="createException" /> with defaulted message.</summary>
    /// <param name="value">Reference-type candidate.</param>
    /// <param name="createException"><c>(message)=&gt;new SomeException(message)</c></param>
    /// <param name="message">Forwarded when null; omit or pass null for a built-in default.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T OrThrow<T>(this T? value, Func<string, Exception> createException, string? message = null)
        where T : class
    {
        if (value is not null)
            return value;

        ArgumentHelpers.ThrowIfNull(createException);
        throw createException(message ?? DefaultRequiredReferenceMissing);
    }

    /// <summary>Returns the contained value when <paramref name="value" /> has a value; otherwise invokes <paramref name="createException" /> with defaulted message.</summary>
    /// <typeparam name="T">Underlying value type.</typeparam>
    /// <param name="value">Nullable value-type candidate.</param>
    /// <param name="createException"><c>(message)=&gt;new SomeException(message)</c></param>
    /// <param name="message">Forwarded when null; omit or pass null for a built-in default.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T OrThrow<T>(this T? value, Func<string, Exception> createException, string? message = null)
        where T : struct
    {
        if (value.HasValue)
            return value.GetValueOrDefault();

        ArgumentHelpers.ThrowIfNull(createException);
        throw createException(message ?? DefaultNullableStructMissing);
    }
}
