using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Lyo.Exceptions.Models;

namespace Lyo.Exceptions;

/// <summary>Helper methods for format validation that throw InvalidFormatException.</summary>
public static class FormatHelpers
{
    private static readonly Regex HexColorRegex = new(@"^#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$", RegexOptions.Compiled);
    private static readonly Regex AlphanumericRegex = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
    private static readonly Regex AlphaRegex = new(@"^[a-zA-Z]+$", RegexOptions.Compiled);
    private static readonly Regex NumericRegex = new(@"^[0-9]+$", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s", RegexOptions.Compiled);

#if NET6_0_OR_GREATER
    [DoesNotReturn]
    [StackTraceHidden]
#endif
    private static void ThrowInvalidFormat(string message, string? paramName, string? invalidValue, params string[] validFormats)
        => throw new InvalidFormatException(message, paramName, invalidValue, validFormats);

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or not a valid GUID.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not a valid GUID format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidGuid([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (!Guid.TryParse(value, out var _))
            ThrowInvalidFormat($"Invalid GUID format: {value}", paramName ?? nameof(value), value, "GUID (e.g., 550e8400-e29b-41d4-a716-446655440000)");
    }

    /// <summary>Validates a string and returns a valid Guid, or throws InvalidFormatException if invalid.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>A valid Guid instance.</returns>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not a valid GUID format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid GetValidGuid([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (Guid.TryParse(value, out var guid))
            return guid;

        ThrowInvalidFormat($"Invalid GUID format: {value}", paramName ?? nameof(value), value, "GUID (e.g., 550e8400-e29b-41d4-a716-446655440000)");
        return default;
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or not a valid hex color (e.g., #000000 or #FFF).</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not a valid hex color format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidHexColor([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (!HexColorRegex.IsMatch(value))
            ThrowInvalidFormat($"Invalid hex color format: {value}", paramName ?? nameof(value), value, "Hex color (e.g., #000000 or #FFF)");
    }

    /// <summary>Validates a hex color string and returns it normalized (with # prefix), or throws InvalidFormatException if invalid.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The normalized hex color string (e.g., #000000).</returns>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not a valid hex color format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetValidHexColor([NotNull] string? value, string? paramName = null)
    {
        ThrowIfInvalidHexColor(value, paramName);
        return value!.StartsWith("#") ? value : "#" + value;
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or not valid Base64.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not valid Base64 format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static void ThrowIfInvalidBase64([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        try {
            Convert.FromBase64String(value!);
        }
        catch (FormatException) {
            ThrowInvalidFormat("Invalid Base64 format.", paramName ?? nameof(value), value, "Base64-encoded string");
        }
    }

    /// <summary>Validates a Base64 string and returns the decoded bytes, or throws InvalidFormatException if invalid.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The decoded byte array.</returns>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not valid Base64 format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetValidBase64([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        try {
            return Convert.FromBase64String(value!);
        }
        catch (FormatException) {
            ThrowInvalidFormat("Invalid Base64 format.", paramName ?? nameof(value), value, "Base64-encoded string");
            return null!;
        }
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or not a valid DateTime.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="formatProvider">Optional format provider for parsing. Defaults to current culture.</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not a valid DateTime format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidDateTime([NotNull] string? value, string? paramName = null, IFormatProvider? formatProvider = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (!DateTime.TryParse(value, formatProvider ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out var _))
            ThrowInvalidFormat($"Invalid DateTime format: {value}", paramName ?? nameof(value), value, "DateTime (e.g., 2024-01-15 or 01/15/2024 14:30:00)");
    }

    /// <summary>Validates a string and returns a valid DateTime, or throws InvalidFormatException if invalid.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="formatProvider">Optional format provider for parsing. Defaults to current culture.</param>
    /// <returns>A valid DateTime instance.</returns>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value is not a valid DateTime format.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetValidDateTime([NotNull] string? value, string? paramName = null, IFormatProvider? formatProvider = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (DateTime.TryParse(value, formatProvider ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTime))
            return dateTime;

        ThrowInvalidFormat($"Invalid DateTime format: {value}", paramName ?? nameof(value), value, "DateTime (e.g., 2024-01-15 or 01/15/2024 14:30:00)");
        return default;
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or does not match the given regex.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="regex">The regex that the value must match. Use ^...$ for full-string matching.</param>
    /// <param name="message">The error message. Use {0} as a placeholder for the invalid value.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="validFormats">Optional descriptions of valid formats (e.g., "Alphanumeric and hyphens only").</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value does not match the regex.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidFormat([NotNull] string? value, Regex regex, string message, string? paramName = null, params string[] validFormats)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (!regex.IsMatch(value!))
            ThrowInvalidFormat(string.Format(message, value), paramName ?? nameof(value), value, validFormats);
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or contains characters other than letters and digits.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value contains invalid characters.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAlphanumeric([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (!AlphanumericRegex.IsMatch(value))
            ThrowInvalidFormat($"Value must contain only alphanumeric characters: {value}", paramName ?? nameof(value), value, "Alphanumeric only (e.g., abc123)");
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or contains characters other than letters.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value contains invalid characters.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAlpha([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (!AlphaRegex.IsMatch(value))
            ThrowInvalidFormat($"Value must contain only letters: {value}", paramName ?? nameof(value), value, "Letters only (e.g., abc)");
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or contains characters other than digits.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value contains invalid characters.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotNumeric([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, paramName ?? nameof(value));
        if (!NumericRegex.IsMatch(value))
            ThrowInvalidFormat($"Value must contain only digits: {value}", paramName ?? nameof(value), value, "Digits only (e.g., 12345)");
    }

    /// <summary>Throws an InvalidFormatException if the string is null, empty, or contains whitespace.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null or empty.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value contains whitespace.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfContainsWhitespace([NotNull] string? value, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(value, paramName ?? nameof(value));
        if (WhitespaceRegex.IsMatch(value!))
            ThrowInvalidFormat($"Value must not contain whitespace: {value}", paramName ?? nameof(value), value, "No whitespace allowed");
    }

    /// <summary>Throws an InvalidFormatException if the string length is outside the specified range.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="minLength">Minimum length (inclusive).</param>
    /// <param name="maxLength">Maximum length (inclusive).</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is null.</exception>
    /// <exception cref="InvalidFormatException">Thrown when value length is outside the range.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidLength([NotNull] string? value, int minLength, int maxLength, string? paramName = null)
    {
        ArgumentHelpers.ThrowIfNull(value, paramName ?? nameof(value));
        var len = value!.Length;
        if (len < minLength || len > maxLength) {
            ThrowInvalidFormat(
                $"Value length {len} is outside valid range [{minLength}, {maxLength}]: {value}", paramName ?? nameof(value), value, $"Length between {minLength} and {maxLength}");
        }
    }

    /// <summary>Throws an InvalidFormatException if the condition is true.</summary>
    /// <param name="condition">The condition to check. If true, an InvalidFormatException is thrown.</param>
    /// <param name="message">The error message.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="invalidValue">The invalid value that caused the exception.</param>
    /// <param name="validFormats">The valid format descriptions or examples.</param>
    /// <exception cref="InvalidFormatException">Thrown when condition is true.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string message, string? paramName = null, string? invalidValue = null, params string[] validFormats)
    {
        if (condition)
            ThrowInvalidFormat(message, paramName, invalidValue, validFormats);
    }
}