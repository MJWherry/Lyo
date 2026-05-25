using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

internal enum StringOrMissingRule
{
    NullOrEmpty,
    NullOrWhitespace,
}

/// <summary>
/// Fluent builder that picks the first non-missing candidate from a chain (nested null-coalescing style), then resolves with <see cref="OrDefault(string)" /> or an <c>OrThrow…</c> terminal.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StringOrExtensions" /> overloads named <c>Or</c> treat <see cref="string.IsNullOrEmpty(string?)" /> as missing.
/// Use <c>OrIfWhiteSpace</c> starters when <see cref="string.IsNullOrWhiteSpace(string?)" /> defines missing instead.
/// </para>
/// </remarks>
public readonly struct StringOrChain
{
    private readonly StringOrMissingRule _missing;
    private readonly string? _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal StringOrChain(StringOrMissingRule missing, string? value)
    {
        _missing = missing;
        _value = value;
    }

    private static bool IsMissing(StringOrMissingRule rule, string? s)
        => rule == StringOrMissingRule.NullOrWhitespace ? string.IsNullOrWhiteSpace(s) : string.IsNullOrEmpty(s);

    /// <summary>
    /// If the current candidate is missing, replaces it with <paramref name="alternative" />; otherwise leaves the chain unchanged.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringOrChain Or(string? alternative)
        => IsMissing(_missing, _value) ? new(_missing, alternative) : this;

    /// <summary>
    /// If the current candidate is missing, invokes <paramref name="alternative" /> once and uses its result; otherwise leaves the chain unchanged.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringOrChain Or(Func<string?> alternative)
    {
        if (!IsMissing(_missing, _value))
            return this;

        ArgumentHelpers.ThrowIfNull(alternative);
        return new(_missing, alternative());
    }

    /// <summary>Returns <c>defaultValue</c> when the accumulated candidate is still missing (see class remarks); otherwise the candidate.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrDefault(string defaultValue = "")
    {
        if (_missing == StringOrMissingRule.NullOrWhitespace)
        {
            if (_value is string sw && !string.IsNullOrWhiteSpace(sw))
                return sw;

            return defaultValue;
        }

        if (_value is string s && !string.IsNullOrEmpty(s))
            return s;

        return defaultValue;
    }

    /// <inheritdoc cref="OrThrowExtensions.OrThrow(System.Func{System.Exception})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrow(Func<Exception> createException)
        => _value.OrThrow(createException);

    /// <inheritdoc cref="OrThrowExtensions.OrThrow(System.Func{System.String,System.Exception},System.String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrow(Func<string, Exception> createException, string? message = null)
        => _value.OrThrow(createException, message);

    /// <inheritdoc cref="OrThrowExtensions.OrThrowIfWhiteSpace(System.Func{System.Exception})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrowIfWhiteSpace(Func<Exception> createException)
        => _value.OrThrowIfWhiteSpace(createException);

    /// <inheritdoc cref="OrThrowExtensions.OrThrowIfWhiteSpace(System.Func{System.String,System.Exception},System.String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrowIfWhiteSpace(Func<string, Exception> createException, string? message = null)
        => _value.OrThrowIfWhiteSpace(createException, message);

    /// <inheritdoc cref="OrThrowExtensions.OrThrowInvalidOperation(System.String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrowInvalidOperation(string? message = null)
        => _value.OrThrowInvalidOperation(message);

    /// <inheritdoc cref="OrThrowExtensions.OrThrowInvalidOperationIfWhiteSpace(System.String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrowInvalidOperationIfWhiteSpace(string? message = null)
        => _value.OrThrowInvalidOperationIfWhiteSpace(message);

    /// <inheritdoc cref="OrThrowExtensions.OrThrowArgument(System.String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrowArgument(string? message = null)
        => _value.OrThrowArgument(message);

    /// <inheritdoc cref="OrThrowExtensions.OrThrowKeyNotFound(System.String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrowKeyNotFound(string? message = null)
        => _value.OrThrowKeyNotFound(message);

    /// <inheritdoc cref="OrThrowExtensions.OrThrowNotSupported(System.String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string OrThrowNotSupported(string? message = null)
        => _value.OrThrowNotSupported(message);
}

/// <summary>Fluent entrypoints for building a <see cref="StringOrChain" />.</summary>
public static class StringOrExtensions
{
    extension(string? first)
    {
        /// <summary>
        /// Starts a chain whose first resolved candidate is <paramref name="first" /> unless it is null or empty—in which case <paramref name="second" /> is tried.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringOrChain Or(string? second)
        {
            var value = string.IsNullOrEmpty(first) ? second : first;
            return new(StringOrMissingRule.NullOrEmpty, value);
        }

        /// <summary>
        /// Starts a chain whose first resolved candidate is <paramref name="first" /> unless it is null or whitespace—in which case <paramref name="second" /> is tried.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringOrChain OrIfWhiteSpace(string? second)
        {
            var value = string.IsNullOrWhiteSpace(first) ? second : first;
            return new(StringOrMissingRule.NullOrWhitespace, value);
        }

        /// <summary>
        /// Starts a chain like <see cref="Or(System.String,System.String)" />, but when <paramref name="first" /> is missing the next candidate comes from invoking <paramref name="second" /> once.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringOrChain Or(Func<string?> second)
        {
            if (!string.IsNullOrEmpty(first))
                return new(StringOrMissingRule.NullOrEmpty, first);

            ArgumentHelpers.ThrowIfNull(second);
            return new(StringOrMissingRule.NullOrEmpty, second());
        }

        /// <summary>
        /// Starts a chain like <see cref="OrIfWhiteSpace(System.String,System.String)" />, but when <paramref name="first" /> is missing the next candidate comes from invoking <paramref name="second" /> once.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringOrChain OrIfWhiteSpace(Func<string?> second)
        {
            if (!string.IsNullOrWhiteSpace(first))
                return new(StringOrMissingRule.NullOrWhitespace, first);

            ArgumentHelpers.ThrowIfNull(second);
            return new(StringOrMissingRule.NullOrWhitespace, second());
        }
    }
}
