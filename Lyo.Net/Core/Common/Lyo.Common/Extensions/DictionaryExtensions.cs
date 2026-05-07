using System.Globalization;

namespace Lyo.Common.Extensions;

/// <summary>Extension methods for reading loosely typed dictionary values as strongly typed scalars.</summary>
public static class DictionaryExtensions
{
    /// <summary>Reads an <see cref="object"/> entry by key, converts it with <see cref="object.ToString"/>, then parses via <see cref="ScalarExtensions.ToScalar{T}(string?, string?, IFormatProvider?)"/>.</summary>
    /// <typeparam name="T">The scalar type to return.</typeparam>
    /// <param name="source">The dictionary.</param>
    /// <param name="key">The key whose value should be read.</param>
    /// <param name="format">Optional format string passed to <see cref="ScalarExtensions.ToScalar{T}(string?, string?, IFormatProvider?)"/>.</param>
    /// <param name="formatProvider">Optional format provider passed to <see cref="ScalarExtensions.ToScalar{T}(string?, string?, IFormatProvider?)"/>.</param>
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
