using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;

namespace Lyo.Query.Services.WhereClause;

/// <summary>Helpers for WhereClause tree walks. Used by QueryService and ProjectionService.</summary>
public static class WhereClauseHelpers
{
    /// <summary>
    /// Maximum nesting depth accepted when walking a clause tree. Recursion past this raises <see cref="InvalidQueryException" /> rather than risking a
    /// <see cref="StackOverflowException" />, which cannot be caught and takes the process down.
    /// </summary>
    public const int MaxClauseDepth = 64;

    /// <summary>
    /// Renders a filter value as a canonical, self-delimiting string for cache keys and fingerprints. Every payload is length-prefixed, so a value containing the separator
    /// cannot forge a different value list, and every rendering is invariant-culture and content-based so two processes and two locales agree.
    /// </summary>
    /// <param name="value">Filter literal: scalar, <see cref="JsonElement" />, or enumerable.</param>
    /// <returns>Canonical string. Distinct values always produce distinct output; equal values always produce equal output.</returns>
    public static string FormatValueCanonical(object? value)
    {
        var sb = new StringBuilder(32);
        AppendValueCanonical(value, sb);
        return sb.ToString();
    }

    /// <summary>Appends <see cref="FormatValueCanonical" /> output to an existing buffer, skipping the intermediate string.</summary>
    /// <param name="value">Filter literal.</param>
    /// <param name="sb">Destination buffer.</param>
    public static void AppendValueCanonical(object? value, StringBuilder sb)
    {
        switch (value) {
            case null:
                sb.Append('n');
                return;
            case string s:
                AppendTaggedText(sb, 's', s);
                return;
            case bool b:
                sb.Append("b:").Append(b ? '1' : '0');
                return;
            case Guid g:
                sb.Append("g:").Append(g.ToString("N", CultureInfo.InvariantCulture));
                return;
            case DateTime dt:
                sb.Append("dt:").Append((int)dt.Kind).Append(':').Append(dt.ToString("O", CultureInfo.InvariantCulture));
                return;
            case DateTimeOffset dto:
                sb.Append("dto:").Append(dto.ToString("O", CultureInfo.InvariantCulture));
                return;
            case TimeSpan ts:
                sb.Append("ts:").Append(ts.ToString("c", CultureInfo.InvariantCulture));
                return;
            case decimal m:
                sb.Append("dec:").Append(m.ToString(CultureInfo.InvariantCulture));
                return;
            case double d:
                sb.Append("f8:").Append(d.ToString("R", CultureInfo.InvariantCulture));
                return;
            case float f:
                sb.Append("f4:").Append(f.ToString("R", CultureInfo.InvariantCulture));
                return;
            case byte[] bytes:
                AppendTaggedText(sb, 'x', ToHex(bytes));
                return;
            case JsonElement je:
                AppendJsonElement(je, sb);
                return;
            case Enum e:
                sb.Append("e:").Append(e.GetType().FullName).Append(':').Append(Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                return;
            case IFormattable formattable when IsIntegral(value):
                sb.Append("i:").Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
        }

#if NET
        switch (value) {
            case DateOnly dateOnly:
                sb.Append("d:").Append(dateOnly.ToString("O", CultureInfo.InvariantCulture));
                return;
            case TimeOnly timeOnly:
                sb.Append("t:").Append(timeOnly.ToString("O", CultureInfo.InvariantCulture));
                return;
        }
#endif

        if (value is IEnumerable enumerable) {
            AppendEnumerable(enumerable, sb);
            return;
        }

        // Unknown scalar: fall back to an invariant rendering tagged with the CLR type so two different types never share a representation.
        var text = value is IFormattable formattableValue ? formattableValue.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
        sb.Append("o:").Append(value.GetType().FullName).Append(':');
        AppendTaggedText(sb, 'v', text ?? "");
    }

    /// <summary>Stable string fingerprint of a where-clause tree for cache keys and logging (not cryptographic).</summary>
    /// <param name="node">Root clause node.</param>
    /// <returns>Concatenated structural string, or empty for unsupported node types.</returns>
    /// <exception cref="InvalidQueryException">The tree nests deeper than <see cref="MaxClauseDepth" />.</exception>
    public static string GetWhereClauseTreeHash(Models.Common.WhereClause node)
    {
        var sb = new StringBuilder(64);
        AppendWhereClauseHash(node, sb);
        return sb.ToString();
    }

    /// <summary>
    /// Appends the canonical fingerprint of <paramref name="node" /> to <paramref name="sb" />. This is the single fingerprint implementation; the predicate cache key, the
    /// include-path cache key, and the API-level result cache keys all resolve here so they cannot drift apart.
    /// </summary>
    /// <param name="node">Clause node to fingerprint.</param>
    /// <param name="sb">Destination buffer.</param>
    /// <param name="depth">Current recursion depth; callers pass 0.</param>
    /// <exception cref="InvalidQueryException">The tree nests deeper than <see cref="MaxClauseDepth" />.</exception>
    public static void AppendWhereClauseHash(Models.Common.WhereClause? node, StringBuilder sb, int depth = 0)
    {
        if (depth > MaxClauseDepth)
            throw new InvalidQueryException($"Where clause nests deeper than the supported limit of {MaxClauseDepth}.");

        switch (node) {
            case ConditionClause c:
                sb.Append("C(").Append(c.Field).Append(c.Comparison);
                AppendValueCanonical(c.Value, sb);
                if (c.SubClause != null) {
                    sb.Append("Sub(");
                    AppendWhereClauseHash(c.SubClause, sb, depth + 1);
                    sb.Append(')');
                }

                sb.Append(')');
                break;
            case GroupClause l:
                sb.Append("L(").Append(l.Operator).Append('[');
                var first = true;
                foreach (var child in l.Children) {
                    if (!first)
                        sb.Append(',');

                    AppendWhereClauseHash(child, sb, depth + 1);
                    first = false;
                }

                sb.Append(']');
                if (l.SubClause != null) {
                    sb.Append("Sub(");
                    AppendWhereClauseHash(l.SubClause, sb, depth + 1);
                    sb.Append(')');
                }

                sb.Append(')');
                break;
        }
    }

    /// <summary>True when the tree contains any <see cref="Lyo.Query.Models.Common.WhereClause.SubClause" />.</summary>
    /// <param name="node">Root node, or <c>null</c>.</param>
    /// <returns><c>true</c> if a sub-clause exists in the tree.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAnySubClause(Models.Common.WhereClause? node)
        => node switch {
            null => false,
            ConditionClause c => c.SubClause != null || HasAnySubClause(c.SubClause),
            GroupClause l => l.SubClause != null || l.Children.Any(HasAnySubClause),
            var _ => false
        };

    /// <summary>Walks every <see cref="ConditionClause" /> in the tree, including those reached through sub-clauses.</summary>
    /// <param name="node">Root node, or <c>null</c>.</param>
    /// <returns>Every condition leaf in evaluation order.</returns>
    /// <exception cref="InvalidQueryException">The tree nests deeper than <see cref="MaxClauseDepth" />.</exception>
    public static IEnumerable<ConditionClause> EnumerateConditions(Models.Common.WhereClause? node) => EnumerateConditions(node, 0);

    /// <summary>Extracts flat conditions from a WhereClause for projection-level filtering. Returns false if SubQueries or unsupported operators are present.</summary>
    /// <param name="node">The root clause, or <c>null</c>.</param>
    /// <param name="conditions">When the method returns <c>true</c>, all <see cref="ConditionClause" /> leaves in evaluation order.</param>
    /// <param name="op">
    /// When the method returns <c>true</c>, the combining operator for a single <see cref="GroupClause" /> (<see cref="GroupOperatorEnum.And" /> or
    /// <see cref="GroupOperatorEnum.Or" />); for a bare condition, <see cref="GroupOperatorEnum.And" />.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="node" /> is null, a single <see cref="ConditionClause" /> without a sub-clause, or an AND/OR group without sub-clauses whose children
    /// flatten recursively; otherwise <c>false</c>.
    /// </returns>
    public static bool TryExtractConditions(Models.Common.WhereClause? node, out List<ConditionClause> conditions, out GroupOperatorEnum op)
    {
        conditions = [];
        op = GroupOperatorEnum.And;
        if (node == null)
            return true;

        if (node is ConditionClause condition) {
            if (condition.SubClause != null)
                return false;

            conditions.Add(condition);
            return true;
        }

        if (node is GroupClause logical) {
            if (logical.SubClause != null)
                return false;

            if (logical.Operator != GroupOperatorEnum.And && logical.Operator != GroupOperatorEnum.Or)
                return false;

            op = logical.Operator;
            foreach (var child in logical.Children) {
                if (!TryExtractConditions(child, out var childConditions, out var _))
                    return false;

                conditions.AddRange(childConditions);
            }

            return true;
        }

        return false;
    }

    private static IEnumerable<ConditionClause> EnumerateConditions(Models.Common.WhereClause? node, int depth)
    {
        if (depth > MaxClauseDepth)
            throw new InvalidQueryException($"Where clause nests deeper than the supported limit of {MaxClauseDepth}.");

        switch (node) {
            case ConditionClause c:
                yield return c;
                if (c.SubClause != null) {
                    foreach (var nested in EnumerateConditions(c.SubClause, depth + 1))
                        yield return nested;
                }

                break;
            case GroupClause l:
                foreach (var child in l.Children) {
                    foreach (var nested in EnumerateConditions(child, depth + 1))
                        yield return nested;
                }

                if (l.SubClause != null) {
                    foreach (var nested in EnumerateConditions(l.SubClause, depth + 1))
                        yield return nested;
                }

                break;
        }
    }

    /// <summary>
    /// A <see cref="JsonElement" /> carries no stable identity — <c>GetHashCode</c> is derived from the backing document and index, so the same literal parsed twice compares
    /// unequal. Fingerprint the raw text instead, and render primitives so they match the equivalent CLR value.
    /// </summary>
    private static void AppendJsonElement(JsonElement je, StringBuilder sb)
    {
        switch (je.ValueKind) {
            case JsonValueKind.String:
                AppendTaggedText(sb, 's', je.GetString() ?? "");
                return;
            case JsonValueKind.True:
                sb.Append("b:1");
                return;
            case JsonValueKind.False:
                sb.Append("b:0");
                return;
            case JsonValueKind.Null or JsonValueKind.Undefined:
                sb.Append('n');
                return;
            default:
                AppendTaggedText(sb, 'j', je.GetRawText());
                return;
        }
    }

    private static void AppendEnumerable(IEnumerable enumerable, StringBuilder sb)
    {
        var start = sb.Length;
        sb.Append("[0:");
        var count = 0;
        foreach (var item in enumerable) {
            if (count > 0)
                sb.Append('|');

            AppendValueCanonical(item, sb);
            count++;
        }

        sb.Append(']');

        // Element count is part of the fingerprint, so an empty tail cannot be confused with a shorter list; patch it in now that it is known.
        if (count != 0)
            sb.Replace("[0:", "[" + count.ToString(CultureInfo.InvariantCulture) + ":", start, 3);
    }

    /// <summary>Length-prefixes free text so a payload containing the list separator cannot be read as two elements.</summary>
    private static void AppendTaggedText(StringBuilder sb, char tag, string text)
        => sb.Append(tag).Append(':').Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);

    private static bool IsIntegral(object value)
        => value is byte or sbyte or short or ushort or int or uint or long or ulong
#if NET
            or Int128 or UInt128
#endif
            ;

    private static string ToHex(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++) {
            var b = bytes[i];
            chars[i * 2] = GetHexDigit(b >> 4);
            chars[(i * 2) + 1] = GetHexDigit(b & 0xF);
        }

        return new(chars);
    }

    private static char GetHexDigit(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));
}
