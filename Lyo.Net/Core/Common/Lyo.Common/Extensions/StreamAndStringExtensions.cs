using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if !NET6_0_OR_GREATER
using System.Text;
#endif
namespace Lyo.Common.Extensions;

/// <summary>Extension methods for <see cref="Stream"/> and string/Guid helpers (position reset, null coalescing, truncation, membership, repetition).</summary>
public static class StreamStringExtensions
{
    /// <summary>Sets the stream position to the beginning when the stream is seekable and not already at the start.</summary>
    /// <param name="source">The stream to rewind.</param>
    /// <param name="throwOnUnSeekable">If <see langword="true"/> and the stream is not seekable but its position is past zero, throws <see cref="InvalidOperationException"/>.</param>
    /// <exception cref="InvalidOperationException">The stream cannot seek, its position is greater than zero, and <paramref name="throwOnUnSeekable"/> is <see langword="true"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MoveToStart(this Stream source, bool throwOnUnSeekable = false)
    {
        source.Position = source switch {
            { CanSeek: true, Position: > 0 } => 0,
            { CanSeek: false, Position: > 0 } when throwOnUnSeekable => throw new InvalidOperationException(""),
            _ => source.Position
        };
    }

    extension([NotNullWhen(false)] string? value)
    {
        /// <summary>Determines whether the string is null or <see cref="string.Empty"/>.</summary>
        /// <returns><see langword="true"/> if <see cref="string.IsNullOrEmpty(string?)"/> would return <see langword="true"/> for this instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullOrEmpty() => string.IsNullOrEmpty(value);

        /// <summary>Determines whether the string is null, empty, or consists only of white-space characters.</summary>
        /// <returns><see langword="true"/> if <see cref="string.IsNullOrWhiteSpace(string?)"/> would return <see langword="true"/> for this instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullOrWhitespace() => string.IsNullOrWhiteSpace(value);
    }

    /// <param name="value">The string to test.</param>
    extension(string? value)
    {
        /// <summary>Returns the default value if the string is null or empty.</summary>
        /// <param name="defaultValue">The value to return when <paramref name="value"/> is null or empty.</param>
        /// <returns><paramref name="defaultValue"/> when the string is null or empty; otherwise <paramref name="value"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrDefault(string defaultValue = "") => value.IsNullOrEmpty() ? defaultValue : value;

        /// <summary>Returns the default value if the string is null, empty, or consists only of whitespace.</summary>
        /// <param name="defaultValue">The value to return when <paramref name="value"/> is null, empty, or whitespace.</param>
        /// <returns><paramref name="defaultValue"/> when the string is null, empty, or whitespace; otherwise <paramref name="value"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string OrDefaultIfWhiteSpace(string defaultValue = "") => value.IsNullOrWhitespace() ? defaultValue : value;
    }

    /// <summary>Builds a shortened string with a leading segment, an ellipsis run, and optionally a trailing segment—useful for masking secrets in logs.</summary>
    /// <param name="s">The source string.</param>
    /// <param name="start">Maximum number of characters to keep from the start; if <see langword="null"/>, the start segment is omitted.</param>
    /// <param name="end">Zero-based index in <paramref name="s"/> where the trailing segment begins; if <see langword="null"/> or past the last character, no trailing segment is appended after the ellipses (unless the whole string fits in the start segment).</param>
    /// <param name="ellipsesLength">Number of <c>'.'</c> characters in the ellipsis between segments.</param>
    /// <returns>An empty string when <paramref name="s"/> is null or empty; otherwise the masked form.</returns>
    public static string Truncated(this string s, in int? start = 4, in int? end = null, in int ellipsesLength = 3)
    {
        if (s.IsNullOrEmpty())
            return string.Empty;

        var ellipses = ".".Repeat(Math.Max(0, ellipsesLength));
        var startLen = start.HasValue ? Math.Min(start.Value, s.Length) : 0;
        var startPart = startLen > 0 ? s[..startLen] : string.Empty;
        if (!end.HasValue || end.Value >= s.Length)
            return startLen >= s.Length ? s : $"{startPart}{ellipses}";

        var endIdx = Math.Max(0, Math.Min(end.Value, s.Length - 1));
        if (endIdx <= startLen)
            return $"{startPart}{ellipses}";

        var endPart = s[endIdx..];
        return $"{startPart}{ellipses}{endPart}";
    }

    /// <inheritdoc cref="Truncated(string, int?, int?, int)"/>
    public static string Truncated(this in Guid guid, int? start = 4, in int? end = null, in int ellipsesLength = 3) => guid.ToString().Truncated(start, end, ellipsesLength);

    /// <inheritdoc cref="Truncated(string, int?, int?, int)"/>
    /// <param name="guid">The GUID to format; if <see langword="null"/>, <see cref="Guid.Empty"/> is used.</param>
    /// <param name="start">Maximum number of characters to keep from the start of the GUID string; see <see cref="Truncated(string, int?, int?, int)"/>.</param>
    /// <param name="end">Index where the trailing segment of the GUID string begins; see <see cref="Truncated(string, int?, int?, int)"/>.</param>
    /// <param name="ellipsesLength">Number of <c>'.'</c> characters between segments; see <see cref="Truncated(string, int?, int?, int)"/>.</param>
    public static string Truncated(this in Guid? guid, in int? start = 4, in int? end = null, in int ellipsesLength = 3)
        => (guid ?? Guid.Empty).Truncated(start, end, ellipsesLength);

    /// <summary>Determines whether this string equals any element of <paramref name="values"/> using the specified comparison.</summary>
    /// <param name="value">The string to find.</param>
    /// <param name="values">The candidate strings.</param>
    /// <param name="comparison">The string comparison to apply.</param>
    /// <returns><see langword="true"/> if any element equals <paramref name="value"/>; otherwise <see langword="false"/>.</returns>
    public static bool In(this string value, in IEnumerable<string> values, StringComparison comparison = StringComparison.CurrentCulture)
        => values.Any(v => v.Equals(value, comparison));

    /// <summary>Concatenates <paramref name="value"/> to itself <paramref name="amount"/> times.</summary>
    /// <param name="value">The string to repeat.</param>
    /// <param name="amount">The repeat count. Non-positive values yield <see cref="string.Empty"/>.</param>
    /// <returns>The repeated string, or <see cref="string.Empty"/> when <paramref name="amount"/> is zero or negative.</returns>
    public static string Repeat(this string value, int amount)
    {
        if (amount <= 0)
            return string.Empty;

        if (amount == 1)
            return value;

        // Single character optimization
        if (value.Length == 1)
            return new(value[0], amount);

#if NET6_0_OR_GREATER
        // Use string.Create for .NET 6+
        return string.Create(
            value.Length * amount, (value, amount), (span, state) => {
                var (str, count) = state;
                for (var i = 0; i < count; i++)
                    str.AsSpan().CopyTo(span.Slice(i * str.Length));
            });
#else
        // Use StringBuilder for .NET Standard 2.0 and earlier
        var sb = new StringBuilder(value.Length * amount);
        for (var i = 0; i < amount; i++)
            sb.Append(value);

        return sb.ToString();
#endif
    }
}
