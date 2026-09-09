using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Records;

namespace Lyo.Parameters;

/// <summary>
/// Typed accessors over any collection of <see cref="ILyoKeyedValue" /> — job run parameters and results, report generation parameters, schedule and trigger overrides. Lookups
/// are case-insensitive and every accessor returns null instead of throwing when the key is absent or the value will not convert.
/// </summary>
/// <remarks>
/// <para>Scalar accessors delegate to <see cref="TypeConversion" />, so booleans parse leniently (<c>1/0</c>, <c>y/n</c>, <c>yes/no</c>, <c>t/f</c>, <c>on/off</c>).</para>
/// <para>
/// The receiver is the interface list rather than a generic list so that <c>GetAs&lt;T&gt;</c> and <c>GetEnum&lt;T&gt;</c> can be called with just their value type argument.
/// <see cref="IReadOnlyList{T}" /> is covariant, so a list of any concrete parameter type binds without a cast.
/// </para>
/// </remarks>
public static class LyoKeyedValueExtensions
{
    extension(IReadOnlyList<ILyoKeyedValue>? items)
    {
        /// <summary>Returns the string stored under <paramref name="key" />, or null when absent.</summary>
        /// <param name="key">Key to look up, matched case-insensitively.</param>
        public string? GetString(string key) => items?.FirstOrDefault(i => i.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

        /// <summary>Returns the int stored under <paramref name="key" />, or null when missing or not parseable.</summary>
        /// <param name="key">Key to look up.</param>
        public int? GetInt(string key)
        {
            var value = items.GetString(key);
            return value != null && TypeConversion.TryConvertTo<int>(value, out var result) ? result : null;
        }

        /// <summary>Returns the long stored under <paramref name="key" />, or null when missing or not parseable.</summary>
        /// <param name="key">Key to look up.</param>
        public long? GetLong(string key)
        {
            var value = items.GetString(key);
            return value != null && TypeConversion.TryConvertTo<long>(value, out var result) ? result : null;
        }

        /// <summary>Returns the decimal stored under <paramref name="key" />, or null when missing or not parseable.</summary>
        /// <param name="key">Key to look up.</param>
        public decimal? GetDecimal(string key)
        {
            var value = items.GetString(key);
            return value != null && TypeConversion.TryConvertTo<decimal>(value, out var result) ? result : null;
        }

        /// <summary>
        /// Returns the bool stored under <paramref name="key" />, or null when missing or not parseable. Accepts every
        /// <see cref="TypeConversion.DefaultTrueValues" /> and <see cref="TypeConversion.DefaultFalseValues" /> token, case-insensitively.
        /// </summary>
        /// <param name="key">Key to look up.</param>
        public bool? GetBool(string key) => TypeConversion.TryToBoolean(items.GetString(key), out var result) ? result : null;

        /// <summary>Returns the <see cref="Guid" /> stored under <paramref name="key" />, or null when missing or not parseable.</summary>
        /// <param name="key">Key to look up.</param>
        public Guid? GetGuid(string key)
        {
            var value = items.GetString(key);
            return value != null && TypeConversion.TryConvertTo<Guid>(value, out var result) ? result : null;
        }

        /// <summary>Returns the <see cref="DateTime" /> stored under <paramref name="key" />, or null when missing or not parseable.</summary>
        /// <param name="key">Key to look up.</param>
        /// <remarks>Parses with <see cref="DateTimeStyles.RoundtripKind" /> rather than <see cref="TypeConversion" />, so round-trip ("O") timestamps keep their UTC kind.</remarks>
        public DateTime? GetDateTime(string key)
        {
            var value = items.GetString(key);
            return DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
        }

        /// <summary>Returns the enum stored under <paramref name="key" /> (case-insensitive), or null when missing or not parseable.</summary>
        /// <param name="key">Key to look up.</param>
        /// <typeparam name="TEnum">Enum type to parse into.</typeparam>
        public TEnum? GetEnum<TEnum>(string key)
            where TEnum : struct, Enum
            => TypeConversion.EnumOrNull<TEnum>(items.GetString(key));

        /// <summary>
        /// Returns a compiled <see cref="Regex" /> from the value stored under <paramref name="key" /> (see <see cref="LyoTypeInfo.Regex" />), or null when missing or
        /// invalid.
        /// </summary>
        /// <param name="key">Key to look up.</param>
        public Regex? GetRegex(string key)
        {
            var value = UnwrapJsonString(items.GetString(key));
            if (string.IsNullOrEmpty(value))
                return null;

            try {
                return new(value);
            }
            catch (ArgumentException) {
                return null;
            }
        }

        /// <summary>
        /// Returns the typed value stored under <paramref name="key" />, or default when missing or not convertible. Supplying <paramref name="format" /> routes straight to
        /// <see cref="ScalarExtensions.ToScalar{TValue}" />; otherwise JSON payloads (see <see cref="LyoTypeInfo.JsonNode" />) deserialize directly and anything else falls back to
        /// <see cref="TypeConversion.ConvertToOrDefault{TValue}" />.
        /// </summary>
        /// <param name="key">Key to look up.</param>
        /// <param name="format">Format string for the format-aware scalar parse. Null uses plain conversion.</param>
        /// <typeparam name="TValue">Type to produce.</typeparam>
        public TValue? GetAs<TValue>(string key, string? format = null)
        {
            var value = items.GetString(key);
            if (value is null)
                return default;

            if (format != null)
                return value.ToScalar<TValue>(format);

            try {
                return JsonSerializer.Deserialize<TValue>(value);
            }
            catch (JsonException) {
                return TypeConversion.ConvertToOrDefault<TValue>(UnwrapJsonString(value));
            }
        }
    }

    /// <summary>Removes the quotes from a JSON string literal, leaving anything else untouched.</summary>
    /// <param name="value">Raw stored value.</param>
    private static string? UnwrapJsonString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        try {
            return JsonSerializer.Deserialize<string>(value!);
        }
        catch (JsonException) {
            return value;
        }
    }
}
