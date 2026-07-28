using System.Globalization;
using System.Text.RegularExpressions;
using Lyo.Common.Conversion;
using Lyo.Common.Extensions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Models.Extensions;

/// <summary>
/// Typed convenience accessors for job run parameter and result collections. These complement the generic <c>GetParameterValueAs&lt;T&gt;</c> /
/// <c>GetResultValueAs&lt;T&gt;</c> methods on <see cref="JobRunRes" /> by exposing well-known scalar types and working directly on the list type, making them usable outside a full
/// <see cref="JobRunRes" /> context.
/// </summary>
/// <remarks>
/// Scalar accessors delegate to <see cref="TypeConversion" />. Booleans parse leniently (<c>1/0</c>, <c>y/n</c>, <c>yes/no</c>, <c>t/f</c>, <c>on/off</c>);
/// <see cref="JobParameterType.Json" /> values deserialize into complex types via <c>GetAs&lt;T&gt;</c>.
/// </remarks>
public static class JobRunParameterExtensions
{
    extension(IReadOnlyList<JobRunParameterRes>? parameters)
    {
        /// <summary>Returns the string value of the parameter with the given key, or null if absent.</summary>
        public string? GetString(string key) => parameters?.FirstOrDefault(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

        /// <summary>Returns the int value of the parameter with the given key, or null if absent / not parseable.</summary>
        public int? GetInt(string key)
        {
            var value = parameters.GetString(key);
            return value != null && TypeConversion.TryConvertTo<int>(value, out var result) ? result : null;
        }

        /// <summary>Returns the long value of the parameter with the given key, or null if absent / not parseable.</summary>
        public long? GetLong(string key)
        {
            var value = parameters.GetString(key);
            return value != null && TypeConversion.TryConvertTo<long>(value, out var result) ? result : null;
        }

        /// <summary>Returns the decimal value of the parameter with the given key, or null if absent / not parseable.</summary>
        public decimal? GetDecimal(string key)
        {
            var value = parameters.GetString(key);
            return value != null && TypeConversion.TryConvertTo<decimal>(value, out var result) ? result : null;
        }

        /// <summary>
        /// Returns the bool value of the parameter with the given key, or null if absent / not parseable. Parses leniently: accepts <see cref="TypeConversion.DefaultTrueValues" />
        /// and <see cref="TypeConversion.DefaultFalseValues" /> tokens (case-insensitive).
        /// </summary>
        public bool? GetBool(string key) => TypeConversion.TryToBoolean(parameters.GetString(key), out var result) ? result : null;

        /// <summary>Returns the <see cref="Guid" /> value of the parameter with the given key, or null if absent / not parseable.</summary>
        public Guid? GetGuid(string key)
        {
            var value = parameters.GetString(key);
            return value != null && TypeConversion.TryConvertTo<Guid>(value, out var result) ? result : null;
        }

        /// <summary>Returns the <see cref="DateTime" /> value of the parameter with the given key, or null if absent / not parseable.</summary>
        /// <remarks>Parses with <see cref="DateTimeStyles.RoundtripKind" /> (not <see cref="TypeConversion" />) so round-trip ("O") timestamps keep their UTC kind.</remarks>
        public DateTime? GetDateTime(string key)
        {
            var value = parameters.GetString(key);
            return DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
        }

        /// <summary>Returns the enum value of the parameter with the given key (case-insensitive), or null if absent / not parseable.</summary>
        public T? GetEnum<T>(string key)
            where T : struct, Enum
            => TypeConversion.EnumOrNull<T>(parameters.GetString(key));

        /// <summary>Returns a compiled <see cref="Regex" /> from the parameter with the given key (see <see cref="JobParameterType.Regex" />), or null if absent / invalid.</summary>
        public Regex? GetRegex(string key)
        {
            var value = parameters.GetString(key);
            if (string.IsNullOrEmpty(value))
                return null;

            try {
                return new(value!);
            }
            catch (ArgumentException) {
                return null;
            }
        }

        /// <summary>
        /// Returns the typed value of the parameter with the given key, or default when absent / not convertible. Scalars convert via
        /// <see cref="TypeConversion.ConvertToOrDefault{T}" />; complex <typeparamref name="T" /> deserializes JSON values (see <see cref="JobParameterType.Json" />). When
        /// <paramref name="format" /> is provided, parsing uses <see cref="StringExtensions.ToScalar{T}" /> (the format-aware path).
        /// </summary>
        public T? GetAs<T>(string key, string? format = null)
        {
            var value = parameters.GetString(key);
            return format != null ? value.ToScalar<T>(format) : TypeConversion.ConvertToOrDefault<T>(value);
        }
    }

    extension(IReadOnlyList<JobRunResultRes>? results)
    {
        /// <summary>Returns the string value of the result with the given key, or null if absent.</summary>
        public string? GetString(string key) => results?.FirstOrDefault(r => r.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

        /// <summary>Returns the int value of the result with the given key, or null if absent / not parseable.</summary>
        public int? GetInt(string key)
        {
            var value = results.GetString(key);
            return value != null && TypeConversion.TryConvertTo<int>(value, out var result) ? result : null;
        }

        /// <summary>Returns the long value of the result with the given key, or null if absent / not parseable.</summary>
        public long? GetLong(string key)
        {
            var value = results.GetString(key);
            return value != null && TypeConversion.TryConvertTo<long>(value, out var result) ? result : null;
        }

        /// <summary>Returns the decimal value of the result with the given key, or null if absent / not parseable.</summary>
        public decimal? GetDecimal(string key)
        {
            var value = results.GetString(key);
            return value != null && TypeConversion.TryConvertTo<decimal>(value, out var result) ? result : null;
        }

        /// <summary>
        /// Returns the bool value of the result with the given key, or null if absent / not parseable. Parses leniently: accepts <see cref="TypeConversion.DefaultTrueValues" /> and
        /// <see cref="TypeConversion.DefaultFalseValues" /> tokens (case-insensitive).
        /// </summary>
        public bool? GetBool(string key) => TypeConversion.TryToBoolean(results.GetString(key), out var result) ? result : null;

        /// <summary>Returns the enum value of the result with the given key (case-insensitive), or null if absent / not parseable.</summary>
        public T? GetEnum<T>(string key)
            where T : struct, Enum
            => TypeConversion.EnumOrNull<T>(results.GetString(key));

        /// <summary>
        /// Returns the typed value of the result with the given key, or default when absent / not convertible. Scalars convert via
        /// <see cref="TypeConversion.ConvertToOrDefault{T}" />; complex <typeparamref name="T" /> deserializes JSON values (see <see cref="JobParameterType.Json" />). When
        /// <paramref name="format" /> is provided, parsing uses <see cref="StringExtensions.ToScalar{T}" /> (the format-aware path).
        /// </summary>
        public T? GetAs<T>(string key, string? format = null)
        {
            var value = results.GetString(key);
            return format != null ? value.ToScalar<T>(format) : TypeConversion.ConvertToOrDefault<T>(value);
        }
    }
}