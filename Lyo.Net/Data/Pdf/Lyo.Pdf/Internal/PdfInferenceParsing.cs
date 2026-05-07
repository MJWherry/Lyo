using Lyo.Pdf.Models;

namespace Lyo.Pdf.Internal;

internal static class PdfInferenceParsing
{
    /// <summary>Bounds key/value vertical band count (netstandard2.0 has no Math.Clamp).</summary>
    internal static int ClampKvColumnBandCount(int value) => value < 1 ? 1 : value > 32 ? 32 : value;

    /// <summary>Distinct printable delimiter characters, order preserved; falls back to <c>:</c> and <c>;</c>.</summary>
    internal static char[] NormalizeKeyValueDelimiters(IReadOnlyList<char>? delimiters)
    {
        const int max = 16;
        if (delimiters is null || delimiters.Count == 0)
            return [':', ';'];

        var list = new List<char>(Math.Min(delimiters.Count, max));
        foreach (var c in delimiters) {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
                continue;

            if (list.Contains(c))
                continue;

            list.Add(c);
            if (list.Count >= max)
                break;
        }

        return list.Count > 0 ? list.ToArray() : [':', ';'];
    }

    internal static bool TryParseDelimiterKeyValueLine(string lineText, char[] delimiters, out string? key, out string? value, out bool labelOnly)
    {
        key = null;
        value = null;
        labelOnly = false;
        var t = lineText.Trim();
        if (string.IsNullOrEmpty(t))
            return false;

        foreach (var d in delimiters.AsSpan()) {
            if (TryParsePunctuationKeyValueLine(t, d, d == ':', out key, out value, out labelOnly))
                return true;
        }

        return false;
    }

    internal static bool TryParsePunctuationKeyValueLine(string t, char delimiter, bool colonUrlGuard, out string? key, out string? value, out bool labelOnly)
    {
        key = null;
        value = null;
        labelOnly = false;
        if (t.IndexOf(delimiter) < 0)
            return false;

        if (colonUrlGuard) {
            if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            if (t.IndexOf("://", StringComparison.Ordinal) >= 0 && t.IndexOf(": ", StringComparison.Ordinal) < 0)
                return false;
        }

        var delimStr = delimiter.ToString();
        var spaced = delimStr + " ";
        var trimmedEnd = t.TrimEnd();
        if (trimmedEnd.EndsWith(delimStr, StringComparison.Ordinal)) {
            var k = (trimmedEnd.Length <= 1 ? string.Empty : trimmedEnd.Substring(0, trimmedEnd.Length - 1)).Trim();
            if (!string.IsNullOrEmpty(k) && LooksPlausibleDelimiterKeyLabel(k)) {
                key = k;
                value = null;
                labelOnly = true;
                return true;
            }
        }

        var sp = t.Split([spaced], 2, StringSplitOptions.None);
        if (sp.Length == 2) {
            var k = sp[0].Trim();
            var v = sp[1].Trim();
            if (!string.IsNullOrEmpty(k) && LooksPlausibleDelimiterKeyLabel(k)) {
                key = k;
                value = v;
                labelOnly = false;
                return true;
            }
        }

        var i = t.IndexOf(delimiter);
        if (i > 0 && i < t.Length - 1) {
            var k = t[..i].Trim();
            var v = t[(i + 1)..].Trim();
            if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v) && LooksPlausibleDelimiterKeyLabel(k)) {
                key = k;
                value = v;
                labelOnly = false;
                return true;
            }
        }

        return false;
    }

    internal static bool LooksPlausibleDelimiterKeyLabel(string k) => k.Any(char.IsLetter);

    internal static string CanonicalInferredKey(string key, char[] delimiterChars)
    {
        var k = key.Trim();
        while (k.Length > 0) {
            var last = k[k.Length - 1];
            if (DelimiterSetIndexOf(delimiterChars, last) < 0)
                break;

            k = k.Substring(0, k.Length - 1).Trim();
        }

        return k;
    }

    internal static int DelimiterSetIndexOf(char[] delimiterChars, char c)
    {
        for (var i = 0; i < delimiterChars.Length; i++) {
            if (delimiterChars[i] == c)
                return i;
        }

        return -1;
    }

    internal static bool LineLooksLikeInferenceHeaderRow(PdfTextLine line, PdfInferFormattingFlags inferFlags)
    {
        if (line.Words.Count == 0)
            return false;

        var emphasized = line.Words.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
        return emphasized / (double)line.Words.Count >= 0.28;
    }

    internal static double InferenceEmphasisRatio(PdfTextLine line, PdfInferFormattingFlags inferFlags)
    {
        if (line.Words.Count == 0)
            return 0;

        var n = line.Words.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
        return n / (double)line.Words.Count;
    }

    internal static bool LineQualifiesForHeaderBlockExtension(PdfTextLine? lineAbove, PdfTextLine nextLine, PdfInferFormattingFlags inferFlags)
    {
        if (nextLine.Words.Count == 0)
            return false;

        if (LineLooksLikeInferenceHeaderRow(nextLine, inferFlags))
            return true;

        if ((inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) == 0)
            return false;

        var nextR = InferenceEmphasisRatio(nextLine, inferFlags);
        if (nextR < 0.07)
            return false;

        if (lineAbove != null) {
            var aboveR = InferenceEmphasisRatio(lineAbove, inferFlags);
            if (aboveR >= 0.20 && nextR >= 0.08)
                return true;
        }

        return nextLine.Words.Any(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags)) && nextR >= 0.10;
    }

    internal static bool LineHasNegligibleInferenceEmphasis(PdfTextLine line, PdfInferFormattingFlags inferFlags)
    {
        if (line.Words.Count == 0)
            return true;

        if ((inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) == 0)
            return false;

        var ratio = line.Words.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags)) / (double)line.Words.Count;
        return ratio < 0.08;
    }

    internal static string[] SplitHeaderLineByDelimiters(string joined, ReadOnlySpan<char> delimiters)
    {
        var t = joined.Trim();
        if (t.Length == 0)
            return [];

        foreach (var d in delimiters) {
            var spaced = d + " ";
            if (d == ':') {
                if (t.IndexOf("://", StringComparison.Ordinal) >= 0)
                    continue;
            }

            if (t.IndexOf(spaced, StringComparison.Ordinal) < 0)
                continue;

            var parts = t.Split([spaced], StringSplitOptions.None).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            if (parts.Length >= 2)
                return parts;
        }

        return [];
    }
}