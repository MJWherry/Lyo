using System.Globalization;
using Lyo.Common.Extensions;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Models.Extensions;

/// <summary>
/// Typed convenience accessors for job run parameter and result collections. These complement the generic <c>GetParameterValueAs&lt;T&gt;</c> /
/// <c>GetResultValueAs&lt;T&gt;</c> methods on <see cref="JobRunRes" /> by exposing well-known scalar types and working directly on the list type, making them usable outside a full
/// <see cref="JobRunRes" /> context.
/// </summary>
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
            return int.TryParse(value, out var result) ? result : null;
        }

        /// <summary>Returns the long value of the parameter with the given key, or null if absent / not parseable.</summary>
        public long? GetLong(string key)
        {
            var value = parameters.GetString(key);
            return long.TryParse(value, out var result) ? result : null;
        }

        /// <summary>Returns the decimal value of the parameter with the given key, or null if absent / not parseable.</summary>
        public decimal? GetDecimal(string key)
        {
            var value = parameters.GetString(key);
            return decimal.TryParse(value, out var result) ? result : null;
        }

        /// <summary>Returns the bool value of the parameter with the given key, or null if absent / not parseable.</summary>
        public bool? GetBool(string key)
        {
            var value = parameters.GetString(key);
            return bool.TryParse(value, out var result) ? result : null;
        }

        /// <summary>Returns the <see cref="Guid" /> value of the parameter with the given key, or null if absent / not parseable.</summary>
        public Guid? GetGuid(string key)
        {
            var value = parameters.GetString(key);
            return Guid.TryParse(value, out var result) ? result : null;
        }

        /// <summary>Returns the <see cref="DateTime" /> value (UTC) of the parameter with the given key, or null if absent / not parseable.</summary>
        public DateTime? GetDateTime(string key)
        {
            var value = parameters.GetString(key);
            return DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
        }

        /// <summary>Returns the enum value of the parameter with the given key (case-insensitive), or null if absent / not parseable.</summary>
        public T? GetEnum<T>(string key)
            where T : struct, Enum
        {
            var value = parameters.GetString(key);
            return Enum.TryParse<T>(value, true, out var result) ? result : null;
        }

        /// <summary>Returns the typed value of the parameter with the given key using <see cref="StringExtensions.ToScalar{T}" />.</summary>
        public T? GetAs<T>(string key, string? format = null) => parameters.GetString(key).ToScalar<T>(format);
    }

    extension(IReadOnlyList<JobRunResultRes>? results)
    {
        /// <summary>Returns the string value of the result with the given key, or null if absent.</summary>
        public string? GetString(string key) => results?.FirstOrDefault(r => r.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

        /// <summary>Returns the int value of the result with the given key, or null if absent / not parseable.</summary>
        public int? GetInt(string key)
        {
            var value = results.GetString(key);
            return int.TryParse(value, out var r) ? r : null;
        }

        /// <summary>Returns the long value of the result with the given key, or null if absent / not parseable.</summary>
        public long? GetLong(string key)
        {
            var value = results.GetString(key);
            return long.TryParse(value, out var r) ? r : null;
        }

        /// <summary>Returns the decimal value of the result with the given key, or null if absent / not parseable.</summary>
        public decimal? GetDecimal(string key)
        {
            var value = results.GetString(key);
            return decimal.TryParse(value, out var r) ? r : null;
        }

        /// <summary>Returns the bool value of the result with the given key, or null if absent / not parseable.</summary>
        public bool? GetBool(string key)
        {
            var value = results.GetString(key);
            return bool.TryParse(value, out var r) ? r : null;
        }

        /// <summary>Returns the enum value of the result with the given key (case-insensitive), or null if absent / not parseable.</summary>
        public T? GetEnum<T>(string key)
            where T : struct, Enum
        {
            var value = results.GetString(key);
            return Enum.TryParse<T>(value, true, out var r) ? r : null;
        }

        /// <summary>Returns the typed value of the result with the given key using <see cref="StringExtensions.ToScalar{T}" />.</summary>
        public T? GetAs<T>(string key, string? format = null) => results.GetString(key).ToScalar<T>(format);
    }
}