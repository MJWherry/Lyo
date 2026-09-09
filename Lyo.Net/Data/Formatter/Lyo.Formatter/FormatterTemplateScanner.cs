using System.Text;

namespace Lyo.Formatter;

/// <summary>Breaks a template into literals and <c>{...}</c> tokens, tagging each placeholder as SmartFormat or a C# expression.</summary>
internal static class FormatterTemplateScanner
{
    private static readonly HashSet<string> TypePrefixes = new(StringComparer.OrdinalIgnoreCase) {
        "DateTime", "DateTimeOffset", "TimeSpan", "Math", "Convert", "string", "String", "Enumerable"
    };

    private static readonly string[] LinqCalls = [
        ".Where(", ".Select(", ".SelectMany(", ".OrderBy(", ".OrderByDescending(", ".ThenBy(", ".ThenByDescending(",
        ".Any(", ".All(", ".Count(", ".Sum(", ".Average(", ".Min(", ".Max(", ".First(", ".FirstOrDefault(",
        ".Last(", ".LastOrDefault(", ".Single(", ".SingleOrDefault(", ".Take(", ".Skip(", ".Distinct(", ".GroupBy(",
        ".ToList(", ".ToArray("
    ];

    /// <summary>Walks <paramref name="template" /> into ordered tokens. An unmatched <c>{</c> yields a single literal of the original text.</summary>
    public static IReadOnlyList<FormatterToken> Scan(string template)
    {
        if (string.IsNullOrEmpty(template))
            return [];

        var tokens = new List<FormatterToken>();
        var literal = new StringBuilder();
        var i = 0;
        while (i < template.Length) {
            if (template[i] == '{' && i + 1 < template.Length && template[i + 1] == '{') {
                literal.Append("{{");
                i += 2;
                continue;
            }

            if (template[i] == '}' && i + 1 < template.Length && template[i + 1] == '}') {
                literal.Append("}}");
                i += 2;
                continue;
            }

            if (template[i] != '{') {
                literal.Append(template[i]);
                i++;
                continue;
            }

            if (!TryReadPlaceholder(template, i, out var end, out var inner)) {
                tokens.Clear();
                tokens.Add(new(FormatterTokenKind.Literal, template, template, null));
                return tokens;
            }

            if (literal.Length > 0) {
                var text = literal.ToString();
                tokens.Add(new(FormatterTokenKind.Literal, text, text, null));
                literal.Clear();
            }

            var raw = template.Substring(i, end - i);
            tokens.Add(Classify(raw, inner));
            i = end;
        }

        if (literal.Length > 0) {
            var text = literal.ToString();
            tokens.Add(new(FormatterTokenKind.Literal, text, text, null));
        }

        return tokens;
    }

    /// <summary>True when any token is an expression (ternary, DateTime, LINQ, or operators).</summary>
    public static bool HasExpression(IReadOnlyList<FormatterToken> tokens) => tokens.Any(token => token.Kind == FormatterTokenKind.Expression);

    internal static FormatterToken Classify(string raw, string inner)
    {
        var working = inner.Trim();
        if (working.Length == 0)
            return new(FormatterTokenKind.SmartFormat, raw, inner, null);

        if (HasQuestionMark(working) || ContainsOperator(working, "=>")) {
            SplitExpressionFormat(working, out var expr, out var spec);
            return new(FormatterTokenKind.Expression, raw, expr, spec);
        }

        var firstColon = FirstDepthZeroColon(working);
        if (firstColon >= 0) {
            var left = working[..firstColon].Trim();
            if (IsPlainSelector(left) && !StartsWithKnownType(left))
                return new(FormatterTokenKind.SmartFormat, raw, inner, null);
        }

        if (LooksLikeExpression(working)) {
            SplitExpressionFormat(working, out var expr, out var spec);
            return new(FormatterTokenKind.Expression, raw, expr, spec);
        }

        return new(FormatterTokenKind.SmartFormat, raw, inner, null);
    }

    private static bool TryReadPlaceholder(string template, int start, out int end, out string inner)
    {
        end = start;
        inner = string.Empty;
        var scan = new TemplateScan();
        var depth = 0;
        for (var i = start; i < template.Length; i++) {
            var c = template[i];
            if (!scan.Consume(c))
                continue;

            if (c == '{' && i + 1 < template.Length && template[i + 1] == '{' && depth == 0) {
                end = -1;
                return false;
            }

            if (c == '{') {
                depth++;
                continue;
            }

            if (c != '}')
                continue;

            depth--;
            if (depth != 0)
                continue;

            end = i + 1;
            inner = template.Substring(start + 1, i - start - 1);
            return true;
        }

        end = -1;
        return false;
    }

    private static bool LooksLikeExpression(string inner)
    {
        if (ContainsThis(inner) || StartsWithKnownType(inner) || ContainsKnownType(inner) || ContainsLinq(inner))
            return true;
        if (ContainsOperator(inner, "==") || ContainsOperator(inner, "!=") || ContainsOperator(inner, ">=") || ContainsOperator(inner, "<="))
            return true;
        if (HasComparison(inner) || HasBinaryArithmetic(inner) || inner.Contains('['))
            return true;
        return false;
    }

    private static void SplitExpressionFormat(string inner, out string expr, out string? spec)
    {
        expr = inner;
        spec = null;
        if (HasDepthZero(inner, '?'))
            return;

        var colon = LastDepthZeroColon(inner);
        if (colon < 0)
            return;

        var right = inner[(colon + 1)..].Trim();
        if (!LooksLikeFormatSpec(right))
            return;

        expr = inner[..colon].Trim();
        spec = right;
    }

    private static bool LooksLikeFormatSpec(string spec)
    {
        if (spec.Length == 0)
            return false;
        foreach (var c in spec) {
            if (char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or ',' or '/' or ':' or ' ' or '#')
                continue;
            return false;
        }

        return true;
    }

    private static bool StartsWithKnownType(string inner)
    {
        var i = 0;
        while (i < inner.Length && (char.IsWhiteSpace(inner[i]) || inner[i] == '('))
            i++;
        var start = i;
        while (i < inner.Length && (char.IsLetterOrDigit(inner[i]) || inner[i] == '_'))
            i++;
        if (i == start)
            return false;
        return TypePrefixes.Contains(inner[start..i]);
    }

    private static bool ContainsKnownType(string inner)
    {
        foreach (var type in TypePrefixes) {
            if (IndexOfWord(inner, type) >= 0)
                return true;
        }

        return false;
    }

    private static int IndexOfWord(string inner, string word)
    {
        var start = 0;
        while (true) {
            var idx = inner.IndexOf(word, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return -1;
            var beforeOk = idx == 0 || !(char.IsLetterOrDigit(inner[idx - 1]) || inner[idx - 1] == '_');
            var after = idx + word.Length;
            var afterOk = after >= inner.Length || !(char.IsLetterOrDigit(inner[after]) || inner[after] == '_');
            if (beforeOk && afterOk)
                return idx;
            start = idx + 1;
        }
    }

    private static bool IsPlainSelector(string text)
    {
        if (text.Length == 0)
            return false;
        var i = 0;
        if (!(char.IsLetter(text[0]) || text[0] == '_'))
            return false;
        i++;
        while (i < text.Length) {
            if (text[i] == '.') {
                i++;
                if (i >= text.Length || !(char.IsLetter(text[i]) || text[i] == '_'))
                    return false;
                i++;
                continue;
            }

            if (!(char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                return false;
            i++;
        }

        return true;
    }

    private static bool ContainsThis(string inner)
    {
        for (var i = 0; i < inner.Length; i++) {
            if (!MatchWord(inner, i, "this"))
                continue;
            var after = i + 4;
            if (after < inner.Length && inner[after] == '.')
                return true;
        }

        return false;
    }

    private static bool ContainsLinq(string inner)
    {
        foreach (var call in LinqCalls) {
            if (inner.IndexOf(call, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static bool HasComparison(string inner)
    {
        var scan = new TemplateScan();
        for (var i = 0; i < inner.Length; i++) {
            var c = inner[i];
            if (!scan.Consume(c))
                continue;

            if (c is '>' or '<' && (i + 1 >= inner.Length || inner[i + 1] != c))
                return true;
        }

        return false;
    }

    private static bool HasBinaryArithmetic(string inner)
    {
        var scan = new TemplateScan();
        for (var i = 0; i < inner.Length; i++) {
            var c = inner[i];
            if (!scan.ConsumeWithDepth(c))
                continue;

            if (i == 0)
                continue;
            if (c is not ('+' or '-' or '*' or '/' or '%'))
                continue;
            if (c == '-' && inner[i - 1] == '(')
                continue;
            if (IsOperandChar(inner[i - 1]) && i + 1 < inner.Length && (IsOperandChar(inner[i + 1]) || inner[i + 1] is '(' or ' ' or '_'))
                return true;
        }

        return false;
    }

    private static bool IsOperandChar(char c) => char.IsLetterOrDigit(c) || c is '_' or ')' or ']';

    private static bool HasDepthZero(string inner, char needle)
    {
        var scan = new TemplateScan();
        foreach (var c in inner) {
            if (scan.ConsumeWithDepth(c) && scan.Depth == 0 && c == needle)
                return true;
        }

        return false;
    }

    /// <summary>True when a <c>?</c> appears outside strings (ternary), including inside parentheses.</summary>
    private static bool HasQuestionMark(string inner)
    {
        var scan = new TemplateScan();
        foreach (var c in inner) {
            if (scan.Consume(c) && c == '?')
                return true;
        }

        return false;
    }

    private static int FirstDepthZeroColon(string inner)
    {
        var scan = new TemplateScan();
        for (var i = 0; i < inner.Length; i++) {
            if (scan.ConsumeWithDepth(inner[i]) && scan.Depth == 0 && inner[i] == ':')
                return i;
        }

        return -1;
    }

    private static int LastDepthZeroColon(string inner)
    {
        var scan = new TemplateScan();
        var last = -1;
        for (var i = 0; i < inner.Length; i++) {
            if (scan.ConsumeWithDepth(inner[i]) && scan.Depth == 0 && inner[i] == ':')
                last = i;
        }

        return last;
    }

    private static bool ContainsOperator(string inner, string op)
    {
        var scan = new TemplateScan();
        for (var i = 0; i <= inner.Length - op.Length; i++) {
            if (scan.ConsumeWithDepth(inner[i]) && scan.Depth == 0 && string.Compare(inner, i, op, 0, op.Length, StringComparison.Ordinal) == 0)
                return true;
        }

        return false;
    }

    private static bool MatchWord(string inner, int index, string word)
    {
        if (index + word.Length > inner.Length)
            return false;
        if (index > 0 && (char.IsLetterOrDigit(inner[index - 1]) || inner[index - 1] == '_'))
            return false;
        if (string.Compare(inner, index, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
            return false;
        var after = index + word.Length;
        return after >= inner.Length || !(char.IsLetterOrDigit(inner[after]) || inner[after] == '_');
    }

    /// <summary>
    /// Walks a template one character at a time, tracking string-literal state, backslash escapes, and bracket depth. Every scan in this class shares it so string-aware skipping
    /// is implemented once.
    /// </summary>
    private struct TemplateScan
    {
        private bool _inString;
        private char _stringChar;
        private bool _escaped;

        /// <summary>Current bracket nesting, maintained only by <see cref="ConsumeWithDepth" />.</summary>
        public int Depth { get; private set; }

        /// <summary>Feeds one character; returns <c>false</c> when it belongs to a string literal (or its quotes) and the caller should skip it.</summary>
        public bool Consume(char c)
        {
            if (_inString) {
                if (_escaped)
                    _escaped = false;
                else if (c == '\\')
                    _escaped = true;
                else if (c == _stringChar)
                    _inString = false;

                return false;
            }

            if (c is '"' or '\'') {
                _inString = true;
                _stringChar = c;
                return false;
            }

            return true;
        }

        /// <summary>As <see cref="Consume" />, but also updates <see cref="Depth" /> for <c>()</c>, <c>[]</c>, and <c>{}</c> pairs before returning.</summary>
        public bool ConsumeWithDepth(char c)
        {
            if (!Consume(c))
                return false;

            if (c is '(' or '[' or '{')
                Depth++;
            else if (c is ')' or ']' or '}' && Depth > 0)
                Depth--;

            return true;
        }
    }
}
