using System.Globalization;

namespace Lyo.Common.Extensions;

/// <summary>
/// Extension methods for parsing strings into scalar types and for reading loosely typed dictionary values as strongly typed scalars.
/// </summary>
public static class ScalarDictionaryExtensions
{
    /// <summary>Parses a string into <typeparamref name="T"/> using invariant culture by default, with special handling for enums and nullable value types.</summary>
    /// <typeparam name="T">The target type; supports <see cref="string"/>, numeric primitives, <see cref="bool"/>, <see cref="Guid"/>, <see cref="DateTime"/>, <see cref="TimeSpan"/>, enums, and other types via <see cref="Convert.ChangeType(object?, Type, IFormatProvider?)"/>.</typeparam>
    /// <param name="value">The string to parse; <see langword="null"/> yields <see langword="default"/>.</param>
    /// <param name="format">Optional format string applied when the parsed value implements <see cref="IFormattable"/> (round-trip formatting then conversion).</param>
    /// <param name="formatProvider">Culture/provider for parsing; defaults to <see cref="CultureInfo.InvariantCulture"/> when <see langword="null"/>.</param>
    /// <returns>The converted value, or <see langword="default"/> when parsing fails or <paramref name="value"/> is <see langword="null"/>.</returns>
    /// <remarks>Failures (including unsupported types or conversion errors) return <see langword="default"/> rather than throwing.</remarks>
    public static T? ToScalar<T>(this string? value, string? format = null, IFormatProvider? formatProvider = null)
    {
        if (value is null)
            return default;

        if (typeof(T) == typeof(string))
            return (T?)(object?)value;

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        formatProvider ??= CultureInfo.InvariantCulture;
        try {
            object? parsed;
            if (targetType == typeof(int))
                parsed = int.TryParse(value, NumberStyles.Any, formatProvider, out var i) ? i : null;
            else if (targetType == typeof(long))
                parsed = long.TryParse(value, NumberStyles.Any, formatProvider, out var l) ? l : null;
            else if (targetType == typeof(double))
                parsed = double.TryParse(value, NumberStyles.Any, formatProvider, out var d) ? d : null;
            else if (targetType == typeof(float))
                parsed = float.TryParse(value, NumberStyles.Any, formatProvider, out var f) ? f : null;
            else if (targetType == typeof(decimal))
                parsed = decimal.TryParse(value, NumberStyles.Any, formatProvider, out var m) ? m : null;
            else if (targetType == typeof(bool))
                parsed = bool.TryParse(value, out var b) ? b : null;
            else if (targetType == typeof(Guid))
                parsed = Guid.TryParse(value, out var g) ? g : null;
            else if (targetType == typeof(DateTime))
                parsed = DateTime.TryParse(value, formatProvider, DateTimeStyles.None, out var dt) ? dt : null;
            else if (targetType == typeof(TimeSpan))
                parsed = TimeSpan.TryParse(value, formatProvider, out var ts) ? ts : null;
            else if (targetType.IsEnum) {
                try {
                    parsed = Enum.Parse(targetType, value);
                }
                catch {
                    parsed = null;
                }
            }
            else
                parsed = Convert.ChangeType(value, targetType, formatProvider);

            if (parsed == null || format == null || parsed is not IFormattable formattable)
                return (T?)parsed;

            var formatted = formattable.ToString(format, formatProvider);
            parsed = Convert.ChangeType(formatted, targetType, formatProvider);
            return (T?)parsed;
        }
        catch {
            return default;
        }
    }

    /// <summary>Reads an <see cref="object"/> entry by key, converts it with <see cref="object.ToString"/>, then parses via <see cref="ToScalar{T}(string?, string?, IFormatProvider?)"/>.</summary>
    /// <typeparam name="T">The scalar type to return.</typeparam>
    /// <param name="source">The dictionary.</param>
    /// <param name="key">The key whose value should be read.</param>
    /// <param name="format">Optional format string passed to <see cref="ToScalar{T}(string?, string?, IFormatProvider?)"/>.</param>
    /// <param name="formatProvider">Optional format provider passed to <see cref="ToScalar{T}(string?, string?, IFormatProvider?)"/>.</param>
    /// <returns>The converted value, or <see langword="default"/> when the key is missing or parsing fails.</returns>
    public static T? GetValueAs<T>(this IDictionary<string, object> source, string key, string? format = null, IFormatProvider? formatProvider = null)
    {
        if (!source.TryGetValue(key, out var v))
            return default;

        formatProvider ??= CultureInfo.InvariantCulture;
        var value = v.ToString();
        return value.ToScalar<T>(format, formatProvider);
    }

    /// <inheritdoc cref="GetValueAs{T}(IDictionary{string, object}, string, string?, IFormatProvider?)"/>
    public static T? GetValueAs<T>(this IReadOnlyDictionary<string, object> source, string key, string? format = null, IFormatProvider? formatProvider = null)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (!source.TryGetValue(key, out var v) || v is null)
            return default;

        formatProvider ??= CultureInfo.InvariantCulture;
        var value = v.ToString();
        return value.ToScalar<T>(format, formatProvider);
    }
}
