using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;
#if !NET6_0_OR_GREATER
using System.Text;
#endif

namespace Lyo.Common.Core.Extensions;

/// <summary>String helpers plus GUID string-formatting helpers.</summary>
public static class StringExtensions
{
    /// <inheritdoc cref="Truncated(string, int?, int?, int)" />
    public static string Truncated(this in Guid guid, int? start = 4, in int? end = null, in int ellipsesLength = 3) => guid.ToString().Truncated(start, end, ellipsesLength);

    /// <inheritdoc cref="Truncated(string, int?, int?, int)" />
    /// <param name="guid">GUID to format; <see cref="Guid.Empty" /> when <see langword="null" />.</param>
    /// <param name="start">Max characters kept from the start of the GUID string; see <see cref="Truncated(string, int?, int?, int)" />.</param>
    /// <param name="end">Index where the trailing GUID segment begins; see <see cref="Truncated(string, int?, int?, int)" />.</param>
    /// <param name="ellipsesLength">Count of <c>'.'</c> characters between segments; see <see cref="Truncated(string, int?, int?, int)" />.</param>
    public static string Truncated(this in Guid? guid, in int? start = 4, in int? end = null, in int ellipsesLength = 3)
        => (guid ?? Guid.Empty).Truncated(start, end, ellipsesLength);

    extension(string value)
    {
        /// <summary>Shortened form with a leading segment, an ellipsis run, and optionally a trailing segment—useful for masking secrets in logs.</summary>
        /// <param name="start">Max characters kept from the start; omit the start segment when <see langword="null" />.</param>
        /// <param name="end">
        /// Zero-based index where the trailing segment begins; if <see langword="null" /> or past the last character, no trailing segment is appended after the
        /// ellipses (unless the whole string fits in the start segment).
        /// </param>
        /// <param name="ellipsesLength">Count of <c>'.'</c> characters in the ellipsis between segments.</param>
        /// <returns>Empty string when the receiver is null or empty; otherwise the masked form.</returns>
        public string Truncated(in int? start = 4, in int? end = null, in int ellipsesLength = 3)
        {
            if (value.IsNullOrEmpty())
                return string.Empty;

            var ellipses = ".".Repeat(Math.Max(0, ellipsesLength));
            var startLen = start.HasValue ? Math.Min(start.Value, value.Length) : 0;
            var startPart = startLen > 0 ? value[..startLen] : string.Empty;
            if (!end.HasValue || end.Value >= value.Length)
                return startLen >= value.Length ? value : $"{startPart}{ellipses}";

            var endIdx = Math.Max(0, Math.Min(end.Value, value.Length - 1));
            if (endIdx <= startLen)
                return $"{startPart}{ellipses}";

            var endPart = value[endIdx..];
            return $"{startPart}{ellipses}{endPart}";
        }

        /// <summary>Truncates the receiver to at most <paramref name="maxLength" /> characters, appending an ellipsis when shortened.</summary>
        /// <param name="maxLength">Max length of the returned string, including the ellipsis suffix.</param>
        /// <returns>
        /// The original string when it fits; otherwise a prefix plus an ellipsis. When <paramref name="maxLength" /> is divisible by 3, the suffix is three ASCII periods
        /// (<c>...</c>), matching <see cref="Truncated(string, int?, int?, int)" />; otherwise the Unicode ellipsis character (<c>…</c>) is used.
        /// </returns>
        public string Ellipsis(int maxLength)
        {
            ArgumentHelpers.ThrowIfLessThan(0, maxLength);
            if (value.Length <= maxLength)
                return value;

            var suffix = maxLength % 3 == 0 ? "..." : "…";
            var prefixLen = Math.Max(0, maxLength - suffix.Length);
            return value[..prefixLen].TrimEnd() + suffix;
        }

        /// <summary>True when this string equals any element of <paramref name="values" /> under the given comparison.</summary>
        /// <param name="values">Candidate strings.</param>
        /// <param name="comparison">String comparison to apply.</param>
        /// <returns><see langword="true" /> if any element equals <paramref name="value" />; otherwise <see langword="false" />.</returns>
        public bool In(in IEnumerable<string> values, StringComparison comparison = StringComparison.CurrentCulture) => values.Any(v => v.Equals(value, comparison));

        /// <summary>Concatenates <paramref name="value" /> to itself <paramref name="amount" /> times.</summary>
        /// <param name="amount">Repeat count. Non-positive values yield <see cref="string.Empty" />.</param>
        /// <returns>The repeated string, or <see cref="string.Empty" /> when <paramref name="amount" /> is zero or negative.</returns>
        public string Repeat(int amount)
        {
            if (amount <= 0)
                return string.Empty;

            if (amount == 1)
                return value;

            // Fast path for a single character
            if (value.Length == 1)
                return new(value[0], amount);

#if NET6_0_OR_GREATER
            // string.Create on .NET 6+
            return string.Create(
                value.Length * amount, (value, amount), (span, state) => {
                    var (str, count) = state;
                    for (var i = 0; i < count; i++)
                        str.AsSpan().CopyTo(span[(i * str.Length)..]);
                });
#else
            // StringBuilder on .NET Standard 2.0 and earlier
            var sb = new StringBuilder(value.Length * amount);
            for (var i = 0; i < amount; i++)
                sb.Append(value);

            return sb.ToString();
#endif
        }
    }

    extension([NotNullWhen(false)] string? value)
    {
        /// <summary>True when the string is null or <see cref="string.Empty" />.</summary>
        /// <returns><see langword="true" /> if <see cref="string.IsNullOrEmpty(string?)" /> would return <see langword="true" /> for this instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullOrEmpty() => string.IsNullOrEmpty(value);

        /// <summary>True when the string is null, empty, or only white-space characters.</summary>
        /// <returns><see langword="true" /> if <see cref="string.IsNullOrWhiteSpace(string?)" /> would return <see langword="true" /> for this instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullOrWhitespace() => string.IsNullOrWhiteSpace(value);
    }

    extension(string? value)
    {
        /// <summary>Returns <paramref name="defaultValue" /> when the string is null or empty.</summary>
        /// <param name="defaultValue">Fallback when the string is null or empty.</param>
        /// <returns><paramref name="defaultValue" /> when the string is null or empty; otherwise the original value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrDefault(string defaultValue = "") => value.IsNullOrEmpty() ? defaultValue : value;

        /// <summary>Returns <paramref name="defaultValue" /> when the string is null, empty, or only whitespace.</summary>
        /// <param name="defaultValue">Fallback when the string is null, empty, or whitespace.</param>
        /// <returns><paramref name="defaultValue" /> when the string is null, empty, or whitespace; otherwise the original value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrDefaultIfWhiteSpace(string defaultValue = "") => value.IsNullOrWhitespace() ? defaultValue : value;

        /// <summary>
        /// Truncates to a total budget of <paramref name="maxLength" /> characters, spending the tail of that budget on <paramref name="ellipsis" /> when shortened.
        /// Null and empty inputs return <see cref="string.Empty" />, so this is safe for UI labels.
        /// </summary>
        /// <param name="maxLength">Total character budget including the ellipsis. Values below 1 return <see cref="string.Empty" />.</param>
        /// <param name="ellipsis">Suffix appended when shortened; truncated itself if the budget is smaller than the suffix.</param>
        /// <returns>The original value when it fits the budget; otherwise a prefix plus <paramref name="ellipsis" />.</returns>
        /// <remarks>
        /// Unlike <see cref="Ellipsis" />, the receiver may be null, and the suffix is fixed rather than chosen from the budget's divisibility by three.
        /// </remarks>
        public string TruncateWithEllipsis(int maxLength, string ellipsis = "...")
        {
            if (string.IsNullOrEmpty(value) || maxLength < 1)
                return string.Empty;

            if (value!.Length <= maxLength)
                return value;

            return maxLength <= ellipsis.Length ? ellipsis[..maxLength] : value[..(maxLength - ellipsis.Length)] + ellipsis;
        }
    }
}