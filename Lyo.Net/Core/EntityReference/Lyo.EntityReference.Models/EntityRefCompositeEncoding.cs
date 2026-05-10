using System.Text;
using Lyo.Exceptions;

namespace Lyo.EntityReference.Models;

/// <summary>
/// Escape and join rules for multi-part <see cref="EntityRef.EntityId"/> values produced by <see cref="EntityRef.For{T}(object[])"/>.
/// The delimiter is an unescaped <c>:</c>; literal backslashes and colons inside a segment are escaped for round-trip safety.
/// </summary>
public static class EntityRefCompositeEncoding
{
    /// <summary>Joins ordered segments into a single composite id (multi-part keys only).</summary>
    /// <param name="orderedSegments">Non-empty list of segments already in canonical order.</param>
    /// <returns>A string containing escaped segments separated by unescaped <c>:</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="orderedSegments"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="orderedSegments"/> is empty.</exception>
    public static string JoinComposite(IReadOnlyList<string> orderedSegments)
    {
        ArgumentHelpers.ThrowIfNull(orderedSegments);
        if (orderedSegments.Count == 0)
            throw new ArgumentException("At least one segment is required.", nameof(orderedSegments));

        return string.Join(":", orderedSegments.Select(EscapeSegment));
    }

    /// <summary>Splits a composite id produced by <see cref="JoinComposite"/> back into segments.</summary>
    /// <param name="composite">Full composite string including delimiters and escapes.</param>
    /// <returns>The ordered segments as originally joined (after unescaping).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="composite"/> is null.</exception>
    public static IReadOnlyList<string> SplitComposite(string composite)
    {
        ArgumentHelpers.ThrowIfNull(composite);
        var segments = new List<string>();
        var sb = new StringBuilder();
        for (var i = 0; i < composite.Length; i++) {
            var c = composite[i];
            if (c == '\\' && i + 1 < composite.Length) {
                var n = composite[i + 1];
                if (n is '\\' or ':') {
                    sb.Append(n);
                    i++;
                    continue;
                }
            }

            if (c == ':') {
                segments.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        segments.Add(sb.ToString());
        return segments;
    }

    /// <summary>Escapes <c>\</c> and <c>:</c> inside a single segment so <see cref="JoinComposite"/> remains reversible.</summary>
    internal static string EscapeSegment(string segment)
        => segment.Replace("\\", "\\\\").Replace(":", "\\:");
}
