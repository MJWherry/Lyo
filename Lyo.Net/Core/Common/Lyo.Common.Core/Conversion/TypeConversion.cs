using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

#if NET10_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Lyo.Common.Core.Conversion;

/// <summary>
/// Shared conversion engine for Lyo. Turns CLR objects, strings, spans, and <see cref="JsonElement" /> values into target types, including nullable unwrapping, enums (name or
/// numeric), and collection materialization. Replaces conversion pipelines that used to be copied across the API, query, and web-component layers.
/// </summary>
/// <remarks>
/// <para>Throwing APIs raise <see cref="TypeConversionException" />; <c>Try*</c> APIs return <see langword="false" /> instead.</para>
/// <para>
/// Failures surface as a return value or exception, not a log line. Log at the call site if you need a record, where surrounding context is available; this package stays free of a
/// logging dependency.
/// </para>
/// <para>Type metadata (nullable unwrapping, enum detection) lives in a static <see cref="ConcurrentDictionary{TKey,TValue}" /> and is immutable per <see cref="Type" />.</para>
/// </remarks>
public static class TypeConversion
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> MetadataCache = new();

    private static readonly string[] DefaultTrueTokens = ["true", "t", "1", "y", "yes", "on"];
    private static readonly string[] DefaultFalseTokens = ["false", "f", "0", "n", "no", "off"];

#if NET10_0_OR_GREATER
    private static readonly FrozenSet<string> TrueTokenSet = DefaultTrueTokens.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly FrozenSet<string> FalseTokenSet = DefaultFalseTokens.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
#else
    private static readonly HashSet<string> TrueTokenSet = new(DefaultTrueTokens, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> FalseTokenSet = new(DefaultFalseTokens, StringComparer.OrdinalIgnoreCase);
#endif

    /// <summary>Lenient boolean tokens treated as <see langword="true" /> (case-insensitive): <c>true, t, 1, y, yes, on</c>.</summary>
    public static IReadOnlyCollection<string> DefaultTrueValues => DefaultTrueTokens;

    /// <summary>Lenient boolean tokens treated as <see langword="false" /> (case-insensitive): <c>false, f, 0, n, no, off</c>.</summary>
    public static IReadOnlyCollection<string> DefaultFalseValues => DefaultFalseTokens;

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // ConvertTo
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Converts a CLR object, string, or <see cref="JsonElement" /> to <paramref name="targetType" />, unwrapping nullable targets.</summary>
    /// <param name="value">Source value; <see langword="null" /> yields <see langword="null" />.</param>
    /// <param name="targetType">Destination type (nullable and enums allowed).</param>
    /// <param name="lenientBoolean">When <see langword="true" />, boolean strings also match <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    /// <returns>Converted value, or <see langword="null" /> when <paramref name="value" /> is <see langword="null" />.</returns>
    /// <exception cref="TypeConversionException">The value cannot become <paramref name="targetType" />.</exception>
    /// <remarks>Strings that look like JSON (<c>{</c> or <c>[</c>) are deserialized into complex targets only after scalar conversion fails.</remarks>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static object? ConvertTo(object? value, Type targetType, bool lenientBoolean = false)
    {
        if (!TryConvertCore(value, targetType, lenientBoolean, out var result, out var error))
            ThrowConversionFailed(value, targetType, error);

        return result;
    }

    /// <inheritdoc cref="ConvertTo(object?, Type, bool)" />
    /// <typeparam name="T">Destination type.</typeparam>
    /// <remarks>Preferred over the <see cref="ReadOnlySpan{T}" /> overload for strings, so strings take the full object pipeline.</remarks>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [OverloadResolutionPriority(1)]
    public static T? ConvertTo<T>(object? value, bool lenientBoolean = false)
    {
        var converted = ConvertTo(value, typeof(T), lenientBoolean);
        // Guard: unboxing null to a non-nullable T? throws, so treat a null object as default
        return converted is null ? default : (T?)converted;
    }

    /// <summary>Parses a character span as <typeparamref name="T" /> (primitives, <see cref="Guid" />, dates/times, enums).</summary>
    /// <typeparam name="T">Destination type (nullable allowed).</typeparam>
    /// <param name="value">Characters to parse.</param>
    /// <param name="lenientBoolean">When <see langword="true" />, boolean parsing also matches <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    /// <exception cref="TypeConversionException">The span cannot be parsed as <typeparamref name="T" />.</exception>
    /// <remarks>net10 uses span-native parsing with no allocation; netstandard2.0 falls back to <see cref="ReadOnlySpan{T}.ToString" />.</remarks>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static T? ConvertTo<T>(ReadOnlySpan<char> value, bool lenientBoolean = false)
        => TryConvertTo<T>(value, out var result, lenientBoolean) ? result : throw ConversionFailed(value.ToString(), typeof(T), null);

    /// <summary>Converts to <typeparamref name="T" />, or <paramref name="defaultValue" /> when the source is <see langword="null" /> or conversion fails.</summary>
    /// <typeparam name="T">Destination type.</typeparam>
    /// <param name="value">Source value.</param>
    /// <param name="defaultValue">Fallback when conversion is not possible.</param>
    /// <param name="lenientBoolean">When <see langword="true" />, boolean strings also match <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? ConvertToOrDefault<T>(object? value, T? defaultValue = default, bool lenientBoolean = false)
    {
        if (value is null)
            return defaultValue;

        return TryConvertTo<T>(value, out var result, lenientBoolean) ? result : defaultValue;
    }

    /// <summary>Tries to convert a value to <paramref name="targetType" /> without throwing.</summary>
    /// <param name="value">Source value; <see langword="null" /> succeeds with a <see langword="null" /> result.</param>
    /// <param name="targetType">Destination type (nullable and enums allowed).</param>
    /// <param name="result">Converted value, or <see langword="null" /> on failure.</param>
    /// <param name="lenientBoolean">When <see langword="true" />, boolean strings also match <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    /// <returns><see langword="true" /> when conversion succeeded.</returns>
    public static bool TryConvertTo(object? value, Type targetType, out object? result, bool lenientBoolean = false)
    {
        if (TryConvertCore(value, targetType, lenientBoolean, out result, out _))
            return true;

        result = null;
        return false;
    }

    /// <inheritdoc cref="TryConvertTo(object?, Type, out object?, bool)" />
    /// <typeparam name="T">Destination type.</typeparam>
    /// <remarks>Preferred over the <see cref="ReadOnlySpan{T}" /> overload for strings, so strings take the full object pipeline.</remarks>
    [OverloadResolutionPriority(1)]
    public static bool TryConvertTo<T>(object? value, out T? result, bool lenientBoolean = false)
    {
        if (TryConvertTo(value, typeof(T), out var converted, lenientBoolean)) {
            // Guard: unboxing null to a non-nullable T? throws, so treat a null object as default
            result = converted is null ? default : (T?)converted;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Tries to parse a character span as <typeparamref name="T" /> without throwing.</summary>
    /// <typeparam name="T">Destination type (nullable allowed).</typeparam>
    /// <param name="value">Characters to parse.</param>
    /// <param name="result">Parsed value, or <see langword="default" /> on failure.</param>
    /// <param name="lenientBoolean">When <see langword="true" />, boolean parsing also matches <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    /// <returns><see langword="true" /> when parsing succeeded.</returns>
    /// <remarks>net10 uses span-native parsing with no allocation; netstandard2.0 falls back to <see cref="ReadOnlySpan{T}.ToString" />.</remarks>
    public static bool TryConvertTo<T>(ReadOnlySpan<char> value, out T? result, bool lenientBoolean = false)
    {
#if NET10_0_OR_GREATER
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var parsed = TryParseSpan(value, targetType, lenientBoolean, out var spanResult);
        if (parsed.HasValue) {
            result = parsed.Value ? (T?)spanResult : default;
            return parsed.Value;
        }
#endif
        return TryConvertTo(value.ToString(), out result, lenientBoolean);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // Collections
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Converts a value to <paramref name="targetType" />, building collection targets (arrays, <see cref="List{T}" />, <see cref="HashSet{T}" />, interfaces such as
    /// <see cref="IReadOnlyList{T}" />/<see cref="ISet{T}" />, and other concrete collections with an <see cref="IEnumerable{T}" /> constructor). A scalar is wrapped when the
    /// target is a collection; each element of an enumerable is converted to the collection element type.
    /// </summary>
    /// <param name="value">Source (scalar, enumerable, or <see cref="JsonElement" />).</param>
    /// <param name="targetType">Destination (scalar or collection).</param>
    /// <param name="lenientBoolean">When <see langword="true" />, boolean strings also match <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" /> and <paramref name="targetType" /> is a non-nullable value type.</exception>
    /// <exception cref="TypeConversionException">An element cannot be converted, or the collection cannot be built.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static object? ConvertToWithCollections(object? value, Type targetType, bool lenientBoolean = false)
    {
        if (value == null) {
            return targetType.IsNullable() || !targetType.IsValueType
                ? null
                : throw new ArgumentNullException(nameof(value), $"Cannot convert null to non-nullable type {targetType.Name}");
        }

        if (!targetType.IsCollectionType() || targetType == typeof(string) || targetType == typeof(byte[]))
            return ConvertTo(value, targetType, lenientBoolean);

        var elementType = targetType.GetCollectionElementType();
        if (value is JsonElement jsonElement) {
            if (jsonElement.ValueKind == JsonValueKind.Array) {
                var convertedElements = jsonElement.EnumerateArray().Select(e => ConvertTo(e, elementType, lenientBoolean)).ToArray();
                return CreateCollectionOfType(targetType, elementType, convertedElements);
            }

            if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return null;
        }

        if (value is IEnumerable enumerable and not string and not byte[]) {
            var convertedValues = enumerable.Cast<object?>().Select(item => ConvertTo(item, elementType, lenientBoolean)).ToArray();
            return CreateCollectionOfType(targetType, elementType, convertedValues);
        }

        // Scalar: wrap it in a one-element collection of the target type
        return CreateCollectionOfType(targetType, elementType, [ConvertTo(value, elementType, lenientBoolean)]);
    }

    /// <summary>Tries <see cref="ConvertToWithCollections" /> without throwing.</summary>
    /// <param name="value">Source (scalar, enumerable, or <see cref="JsonElement" />).</param>
    /// <param name="targetType">Destination (scalar or collection).</param>
    /// <param name="result">Converted value, or <see langword="null" /> on failure.</param>
    /// <param name="lenientBoolean">When <see langword="true" />, also match <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    /// <returns><see langword="true" /> when conversion succeeded.</returns>
    public static bool TryConvertToWithCollections(object? value, Type targetType, out object? result, bool lenientBoolean = false)
    {
        try {
            result = ConvertToWithCollections(value, targetType, lenientBoolean);
            return true;
        }
        catch (Exception) {
            result = null;
            return false;
        }
    }

    /// <summary>Turns any value into a sequence: <see cref="JsonElement" /> arrays expand per element, enumerables are walked, scalars are wrapped.</summary>
    /// <param name="value">Source; <see langword="null" /> (and JSON null/undefined) yields an empty sequence.</param>
    /// <returns>A materialized sequence of loose values (strings and byte arrays stay scalars, not sequences).</returns>
    public static IEnumerable<object?> ToEnumerable(object? value)
        => value switch {
            null => [],
            JsonElement { ValueKind: JsonValueKind.Array } element => element.EnumerateArray().Select(e => FromJsonElement(e)).ToList(),
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => [],
            JsonElement element => [FromJsonElement(in element)],
            string or byte[] => [value],
            IEnumerable enumerable => enumerable.Cast<object?>(),
            var _ => [value]
        };

    /// <summary>Converts each sequence element to <typeparamref name="T" />.</summary>
    /// <typeparam name="T">Element destination type.</typeparam>
    /// <param name="values">Source values; <see langword="null" /> yields an empty array.</param>
    /// <param name="lenientBoolean">When <see langword="true" />, also match <see cref="DefaultTrueValues" />/<see cref="DefaultFalseValues" />.</param>
    /// <exception cref="TypeConversionException">An element cannot be converted.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static T?[] ConvertToArray<T>(IEnumerable<object?>? values, bool lenientBoolean = false)
        => values == null ? [] : values.Select(v => ConvertTo<T>(v, lenientBoolean)).ToArray();

    /// <inheritdoc cref="ConvertToArray{T}" />
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static List<T?> ConvertToList<T>(IEnumerable<object?>? values, bool lenientBoolean = false)
        => values == null ? [] : values.Select(v => ConvertTo<T>(v, lenientBoolean)).ToList();

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // JsonElement
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Loose CLR value from a <see cref="JsonElement" /> (string, int/long/double, bool, <see langword="null" />, or a list for arrays).</summary>
    /// <param name="element">Element to extract.</param>
    /// <returns>Extracted value; objects and unknown kinds yield the raw JSON text.</returns>
    public static object? FromJsonElement(in JsonElement element)
        => element.ValueKind switch {
            JsonValueKind.Array => element.EnumerateArray().Select(e => FromJsonElement(e)).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            var _ => element.GetRawText()
        };

    /// <summary>
    /// Converts a <see cref="JsonElement" /> to <paramref name="targetType" /> with strict typed accessors (a string token is not turned into a number, and similar), then a
    /// serializer fallback for complex types. For lenient tokens use <see cref="ConvertTo(object?, Type, bool)" />.
    /// </summary>
    /// <param name="element">Element to convert.</param>
    /// <param name="targetType">Destination (nullable and enums allowed; enums accept names or numeric values).</param>
    /// <exception cref="TypeConversionException">The element cannot become <paramref name="targetType" />.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static object FromJsonElement(in JsonElement element, Type targetType)
    {
        if (TryFromJsonElementCore(in element, targetType, GetMetadata(targetType), out var result, out var error) && result != null)
            return result;

        throw ConversionFailed(element, targetType, error);
    }

    /// <summary>Tries to convert a <see cref="JsonElement" /> to <paramref name="targetType" /> with strict typed accessors; returns <see langword="false" /> instead of throwing.</summary>
    /// <param name="element">Element to convert.</param>
    /// <param name="targetType">Destination (nullable and enums allowed).</param>
    /// <param name="result">Converted value, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> when conversion succeeded.</returns>
    public static bool TryFromJsonElement(in JsonElement element, Type targetType, out object? result)
    {
        if (TryFromJsonElementCore(in element, targetType, GetMetadata(targetType), out result, out _) && result != null)
            return true;

        result = null;
        return false;
    }

    /// <inheritdoc cref="TryFromJsonElement(in JsonElement, Type, out object?)" />
    /// <typeparam name="T">Destination type.</typeparam>
    public static bool TryFromJsonElement<T>(in JsonElement element, out T? value)
    {
        if (TryFromJsonElement(in element, typeof(T), out var result)) {
            value = (T?)result;
            return true;
        }

        value = default;
        return false;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // Enums
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Parses an enum from a string (case-insensitive), or <paramref name="defaultValue" /> when parse fails or the text is <see langword="null" />.</summary>
    /// <typeparam name="TEnum">Enum type.</typeparam>
    /// <param name="value">Enum name or numeric string.</param>
    /// <param name="defaultValue">Fallback when parsing fails.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum EnumOrDefault<TEnum>(string? value, TEnum defaultValue = default)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : defaultValue;

    /// <summary>Parses an enum from a string (case-insensitive), or <see langword="null" /> when parse fails or the text is <see langword="null" />.</summary>
    /// <typeparam name="TEnum">Enum type.</typeparam>
    /// <param name="value">Enum name or numeric string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum? EnumOrNull<TEnum>(string? value)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : null;

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // Booleans
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Parses a boolean leniently. Default tokens are <see cref="DefaultTrueValues" /> and <see cref="DefaultFalseValues" /> (case-insensitive).</summary>
    /// <param name="value">String to parse.</param>
    /// <param name="trueValues">Optional <see langword="true" /> tokens (case-insensitive); <see langword="null" /> uses <see cref="DefaultTrueValues" />.</param>
    /// <param name="falseValues">Optional <see langword="false" /> tokens (case-insensitive); <see langword="null" /> uses <see cref="DefaultFalseValues" />.</param>
    /// <exception cref="TypeConversionException">The value is not a recognized boolean token.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static bool ToBoolean(string value, IReadOnlyCollection<string>? trueValues = null, IReadOnlyCollection<string>? falseValues = null)
        => TryToBoolean(value, out var result, trueValues, falseValues) ? result : throw ConversionFailed(value, typeof(bool), null);

    /// <summary>Parses a character span as a lenient boolean with the default token sets.</summary>
    /// <param name="value">Characters to parse.</param>
    /// <exception cref="TypeConversionException">The characters are not a recognized boolean token.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static bool ToBoolean(ReadOnlySpan<char> value) => TryToBoolean(value, out var result) ? result : throw ConversionFailed(value.ToString(), typeof(bool), null);

    /// <summary>Tries to parse a boolean leniently without throwing. Default tokens are <see cref="DefaultTrueValues" /> and <see cref="DefaultFalseValues" />.</summary>
    /// <param name="value">String to parse; <see langword="null" /> returns <see langword="false" />.</param>
    /// <param name="result">Parsed value; <see langword="false" /> when parsing failed.</param>
    /// <param name="trueValues">Optional <see langword="true" /> tokens (case-insensitive); <see langword="null" /> uses <see cref="DefaultTrueValues" />.</param>
    /// <param name="falseValues">Optional <see langword="false" /> tokens (case-insensitive); <see langword="null" /> uses <see cref="DefaultFalseValues" />.</param>
    /// <returns><see langword="true" /> when the value was a recognized token.</returns>
    public static bool TryToBoolean(string? value, out bool result, IReadOnlyCollection<string>? trueValues = null, IReadOnlyCollection<string>? falseValues = null)
    {
        result = false;
        if (string.IsNullOrEmpty(value))
            return false;

        if (trueValues == null ? TrueTokenSet.Contains(value!) : trueValues.Contains(value!, StringComparer.OrdinalIgnoreCase)) {
            result = true;
            return true;
        }

        if (falseValues == null ? FalseTokenSet.Contains(value!) : falseValues.Contains(value!, StringComparer.OrdinalIgnoreCase)) {
            result = false;
            return true;
        }

        return bool.TryParse(value, out result);
    }

    /// <summary>Tries to parse a character span as a lenient boolean with the default tokens, without throwing (no allocation on net10).</summary>
    /// <param name="value">Characters to parse.</param>
    /// <param name="result">Parsed value; <see langword="false" /> when parsing failed.</param>
    /// <returns><see langword="true" /> when the span was a recognized token.</returns>
    public static bool TryToBoolean(ReadOnlySpan<char> value, out bool result)
    {
#if NET10_0_OR_GREATER
        result = false;
        if (value.IsEmpty)
            return false;

        foreach (var token in DefaultTrueTokens) {
            if (value.Equals(token, StringComparison.OrdinalIgnoreCase)) {
                result = true;
                return true;
            }
        }

        foreach (var token in DefaultFalseTokens) {
            if (value.Equals(token, StringComparison.OrdinalIgnoreCase)) {
                result = false;
                return true;
            }
        }

        return bool.TryParse(value, out result);
#else
        return TryToBoolean(value.ToString(), out result);
#endif
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // Type helpers
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Underlying type when nullable; otherwise the type itself (cached).</summary>
    /// <param name="type">Type to unwrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type GetUnderlyingType(Type type) => GetMetadata(type).UnderlyingType;

    /// <summary>True when the object is enumerable and is not a string or byte[].</summary>
    /// <param name="obj">Object to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsObjectEnumerable(object? obj) => obj is not null and not string and not byte[] and IEnumerable;

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // Core (non-throwing) pipeline
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    private static TypeMetadata GetMetadata(Type type)
        => MetadataCache.GetOrAdd(
            type, static t => {
                var underlyingType = Nullable.GetUnderlyingType(t) ?? t;
                var isEnum = underlyingType.IsEnum;
                return new(underlyingType, isEnum, underlyingType != t, isEnum ? underlyingType : null);
            });

    private static bool TryConvertCore(object? value, Type targetType, bool lenientBoolean, out object? result, out Exception? error)
    {
        error = null;
        switch (value) {
            case null:
                result = null;
                return true;
            case var _ when targetType.IsInstanceOfType(value):
                result = value;
                return true;
            case byte[]:
                result = value;
                return true;
        }

        var metadata = GetMetadata(targetType);
        if (value is JsonElement element)
            return TryConvertJsonElement(in element, targetType, metadata, lenientBoolean, out result, out error);

#if NET10_0_OR_GREATER
        if (metadata.UnderlyingType == typeof(DateOnly)) {
            if (value is string dateString && DateOnly.TryParse(dateString, out var dateOnly)) {
                result = dateOnly;
                return true;
            }

            result = null;
            return false;
        }

        if (metadata.UnderlyingType == typeof(TimeOnly)) {
            if (value is string timeString && TimeOnly.TryParse(timeString, out var timeOnly)) {
                result = timeOnly;
                return true;
            }

            result = null;
            return false;
        }
#endif
        if (metadata.UnderlyingType == typeof(Guid)) {
            if (value is string guidString && Guid.TryParse(guidString, out var guid)) {
                result = guid;
                return true;
            }

            result = null;
            return false;
        }

        if (metadata.IsEnum)
            return TryConvertToEnum(value, metadata.EnumType!, out result, out error);

        if (value is string str)
            return TryConvertString(str, metadata.UnderlyingType, lenientBoolean, out result, out error);

        try {
            result = Convert.ChangeType(value, metadata.UnderlyingType);
            return true;
        }
        catch (Exception ex) {
            error = ex;
            result = null;
            return false;
        }
    }

    private static bool TryConvertJsonElement(in JsonElement element, Type targetType, TypeMetadata metadata, bool lenientBoolean, out object? result, out Exception? error)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) {
            result = null;
            error = null;
            return true;
        }

        if (TryFromJsonElementCore(in element, targetType, metadata, out result, out error))
            return true;

        // Kind does not match the typed accessor (for example "123" for an int): take the loose value and convert that.
        if (element.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            return TryConvertCore(FromJsonElement(in element), targetType, lenientBoolean, out result, out error);

        return false;
    }

    /// <summary>Strict typed conversion: element kind must match the target (no string-to-number coercion). Enums accept name or numeric tokens.</summary>
    private static bool TryFromJsonElementCore(in JsonElement element, Type targetType, TypeMetadata metadata, out object? result, out Exception? error)
    {
        error = null;
        result = null;

        // Enums report TypeCode.Int32; skip numeric accessors on string tokens and accept a name or numeric value.
        if (metadata.IsEnum)
            return TryConvertJsonElementToEnum(in element, metadata.EnumType!, out result, out error);

        var kind = element.ValueKind;
        switch (Type.GetTypeCode(metadata.UnderlyingType)) {
            case TypeCode.Boolean:
                if (kind is JsonValueKind.True or JsonValueKind.False) {
                    result = kind == JsonValueKind.True;
                    return true;
                }

                return false;
            case TypeCode.Byte:
                if (kind == JsonValueKind.Number && element.TryGetByte(out var b)) {
                    result = b;
                    return true;
                }

                return false;
            case TypeCode.SByte:
                if (kind == JsonValueKind.Number && element.TryGetSByte(out var sb)) {
                    result = sb;
                    return true;
                }

                return false;
            case TypeCode.Int16:
                if (kind == JsonValueKind.Number && element.TryGetInt16(out var i16)) {
                    result = i16;
                    return true;
                }

                return false;
            case TypeCode.UInt16:
                if (kind == JsonValueKind.Number && element.TryGetUInt16(out var u16)) {
                    result = u16;
                    return true;
                }

                return false;
            case TypeCode.Int32:
                if (kind == JsonValueKind.Number && element.TryGetInt32(out var i32)) {
                    result = i32;
                    return true;
                }

                return false;
            case TypeCode.UInt32:
                if (kind == JsonValueKind.Number && element.TryGetUInt32(out var u32)) {
                    result = u32;
                    return true;
                }

                return false;
            case TypeCode.Int64:
                if (kind == JsonValueKind.Number && element.TryGetInt64(out var i64)) {
                    result = i64;
                    return true;
                }

                return false;
            case TypeCode.UInt64:
                if (kind == JsonValueKind.Number && element.TryGetUInt64(out var u64)) {
                    result = u64;
                    return true;
                }

                return false;
            case TypeCode.Single:
                if (kind == JsonValueKind.Number && element.TryGetSingle(out var f)) {
                    result = f;
                    return true;
                }

                return false;
            case TypeCode.Double:
                if (kind == JsonValueKind.Number && element.TryGetDouble(out var d)) {
                    result = d;
                    return true;
                }

                return false;
            case TypeCode.Decimal:
                if (kind == JsonValueKind.Number && element.TryGetDecimal(out var m)) {
                    result = m;
                    return true;
                }

                return false;
            case TypeCode.DateTime:
                if (kind == JsonValueKind.String && element.TryGetDateTime(out var dt)) {
                    result = dt;
                    return true;
                }

                return false;
            case TypeCode.String:
                if (kind == JsonValueKind.String) {
                    result = element.GetString();
                    return true;
                }

                return false;
        }

        if (metadata.UnderlyingType == typeof(DateTimeOffset)) {
            if (kind == JsonValueKind.String && element.TryGetDateTimeOffset(out var dto)) {
                result = dto;
                return true;
            }

            return false;
        }

        if (metadata.UnderlyingType == typeof(Guid)) {
            if (kind == JsonValueKind.String && element.TryGetGuid(out var guid)) {
                result = guid;
                return true;
            }

            return false;
        }

        try {
            result = element.Deserialize(targetType);
            return result != null;
        }
        catch (Exception ex) {
            error = ex;
            return false;
        }
    }

    private static bool TryConvertJsonElementToEnum(in JsonElement element, Type enumType, out object? result, out Exception? error)
    {
        error = null;
        result = null;
        switch (element.ValueKind) {
            case JsonValueKind.String:
                return TryConvertToEnum(element.GetString()!, enumType, out result, out error);
            case JsonValueKind.Number:
                // Prefer integer forms; fall back to double when the JSON number is not integral.
                if (element.TryGetInt64(out var int64)) {
                    result = Enum.ToObject(enumType, int64);
                    return true;
                }

                if (element.TryGetDouble(out var dbl)) {
                    result = Enum.ToObject(enumType, Convert.ChangeType(dbl, Enum.GetUnderlyingType(enumType)));
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    /// <summary>Converts a value to an enum. Accepts names (case-insensitive), numeric strings, and integral/numeric CLR values.</summary>
    private static bool TryConvertToEnum(object value, Type enumType, out object? result, out Exception? error)
    {
        error = null;
        result = null;
        if (value is string stringValue) {
#if NETSTANDARD2_0
            // netstandard2.0 has no non-generic Enum.TryParse(Type, ...); Enum.Parse accepts names ("Success") and numeric strings ("1").
            try {
                result = Enum.Parse(enumType, stringValue, true);
                return true;
            }
            catch (Exception ex) {
                error = ex;
                return false;
            }
#else
            if (Enum.TryParse(enumType, stringValue, ignoreCase: true, out result))
                return true;

            return false;
#endif
        }

        try {
            var underlying = Enum.GetUnderlyingType(enumType);
            var numeric = value is IConvertible ? Convert.ChangeType(value, underlying) : value;
            result = Enum.ToObject(enumType, numeric);
            return true;
        }
        catch (Exception ex) {
            error = ex;
            return false;
        }
    }

    private static bool TryConvertString(string str, Type underlyingType, bool lenientBoolean, out object? result, out Exception? error)
    {
        error = null;
        switch (Type.GetTypeCode(underlyingType)) {
            case TypeCode.Int32:
                result = int.TryParse(str, out var i) ? i : null;
                return result != null;
            case TypeCode.Int64:
                result = long.TryParse(str, out var l) ? l : null;
                return result != null;
            case TypeCode.Int16:
                result = short.TryParse(str, out var s) ? s : null;
                return result != null;
            case TypeCode.Decimal:
                result = decimal.TryParse(str, out var m) ? m : null;
                return result != null;
            case TypeCode.Double:
                result = double.TryParse(str, out var d) ? d : null;
                return result != null;
            case TypeCode.Single:
                result = float.TryParse(str, out var f) ? f : null;
                return result != null;
            case TypeCode.DateTime:
                result = DateTime.TryParse(str, out var dt) ? dt : null;
                return result != null;
            case TypeCode.Boolean:
                if (lenientBoolean ? TryToBoolean(str, out var lenient) : bool.TryParse(str, out lenient)) {
                    result = lenient;
                    return true;
                }

                result = null;
                return false;
            case TypeCode.String:
                result = str;
                return true;
        }

        if (underlyingType == typeof(Guid)) {
            result = Guid.TryParse(str, out var g) ? g : null;
            return result != null;
        }

        if (underlyingType == typeof(DateTimeOffset)) {
            result = DateTimeOffset.TryParse(str, out var dto) ? dto : null;
            return result != null;
        }

#if NET10_0_OR_GREATER
        if (underlyingType == typeof(DateOnly)) {
            result = DateOnly.TryParse(str, out var dateOnly) ? dateOnly : null;
            return result != null;
        }

        if (underlyingType == typeof(TimeOnly)) {
            result = TimeOnly.TryParse(str, out var timeOnly) ? timeOnly : null;
            return result != null;
        }
#endif
        try {
            result = Convert.ChangeType(str, underlyingType);
            return true;
        }
        catch (Exception ex) {
            error = ex;
            result = null;
        }

        // Last-resort deserialize when the string looks like JSON (for example a serialized parameter payload).
        // Reached only after every conversion above failed, so previously-succeeding paths stay unchanged.
        if (LooksLikeJson(str)) {
            try {
                result = JsonSerializer.Deserialize(str, underlyingType);
                if (result != null) {
                    error = null;
                    return true;
                }
            }
            catch (Exception ex) {
                error = ex;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LooksLikeJson(string str)
    {
        for (var i = 0; i < str.Length; i++) {
            var c = str[i];
            if (char.IsWhiteSpace(c))
                continue;

            return c is '{' or '[';
        }

        return false;
    }

#if NET10_0_OR_GREATER
    /// <summary>Span-native parse for common scalars. Returns null when there is no span fast path (caller uses the string pipeline).</summary>
    private static bool? TryParseSpan(ReadOnlySpan<char> value, Type targetType, bool lenientBoolean, out object? result)
    {
        result = null;
        if (targetType == typeof(string)) {
            result = value.ToString();
            return true;
        }

        if (targetType == typeof(int)) {
            if (int.TryParse(value, out var i))
                result = i;
            return result != null;
        }

        if (targetType == typeof(long)) {
            if (long.TryParse(value, out var l))
                result = l;
            return result != null;
        }

        if (targetType == typeof(short)) {
            if (short.TryParse(value, out var s))
                result = s;
            return result != null;
        }

        if (targetType == typeof(double)) {
            if (double.TryParse(value, out var d))
                result = d;
            return result != null;
        }

        if (targetType == typeof(float)) {
            if (float.TryParse(value, out var f))
                result = f;
            return result != null;
        }

        if (targetType == typeof(decimal)) {
            if (decimal.TryParse(value, out var m))
                result = m;
            return result != null;
        }

        if (targetType == typeof(bool)) {
            if (lenientBoolean ? TryToBoolean(value, out var b) : bool.TryParse(value, out b))
                result = b;
            return result != null;
        }

        if (targetType == typeof(Guid)) {
            if (Guid.TryParse(value, out var g))
                result = g;
            return result != null;
        }

        if (targetType == typeof(DateTime)) {
            if (DateTime.TryParse(value, out var dt))
                result = dt;
            return result != null;
        }

        if (targetType == typeof(DateTimeOffset)) {
            if (DateTimeOffset.TryParse(value, out var dto))
                result = dto;
            return result != null;
        }

        if (targetType == typeof(DateOnly)) {
            if (DateOnly.TryParse(value, out var dateOnly))
                result = dateOnly;
            return result != null;
        }

        if (targetType == typeof(TimeOnly)) {
            if (TimeOnly.TryParse(value, out var timeOnly))
                result = timeOnly;
            return result != null;
        }

        if (targetType.IsEnum)
            return Enum.TryParse(targetType, value, ignoreCase: true, out result);

        // No span fast path; caller uses the string pipeline.
        return null;
    }
#endif

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // Collection materialization
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Builds converted values as an instance assignable to the target collection (array, List&lt;T&gt;, HashSet&lt;T&gt;, interfaces, and similar).</summary>
    private static object CreateCollectionOfType(Type collectionType, Type elementType, object?[] values)
    {
        var array = Array.CreateInstance(elementType, values.Length);
        for (var i = 0; i < values.Length; i++)
            array.SetValue(values[i], i);

        if (collectionType.IsInstanceOfType(array))
            return array;

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (var value in values)
            list.Add(value);

        if (collectionType.IsInstanceOfType(list))
            return list;

        // ISet<T> / IReadOnlySet<T> are not satisfied by array or list; build a HashSet<T>
        if (collectionType.IsInterface) {
            var hashSet = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType), list)!;
            if (collectionType.IsInstanceOfType(hashSet))
                return hashSet;
        }

        // Concrete collections that take IEnumerable<T> in a constructor (for example HashSet<T>, Queue<T>)
        var ctor = collectionType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)]);
        if (ctor != null)
            return ctor.Invoke([list]);

        throw ConversionFailed(values, collectionType, null);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------------------
    // Throw helpers
    // ---------------------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>Builds the failed-conversion exception. Callers <see langword="throw" /> the result so it works in expression contexts.</summary>
    private static TypeConversionException ConversionFailed(object? value, Type targetType, Exception? inner)
    {
        var sourceTypeName = value?.GetType().Name ?? "null";
        return new($"Cannot convert value '{value}' of type '{sourceTypeName}' to type '{targetType.Name}'.", value, targetType, inner);
    }

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowConversionFailed(object? value, Type targetType, Exception? inner) => throw ConversionFailed(value, targetType, inner);


    private sealed record TypeMetadata(Type UnderlyingType, bool IsEnum, bool IsNullable, Type? EnumType);
}