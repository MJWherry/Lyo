using System.Text;

namespace Lyo.Web.Components;

internal static class ElementIdSegmentNormalizer
{
    public static string NormalizeOrDefault(string? value, string fallback = "default")
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var trimmed = value.Trim();
        var sb = new StringBuilder(trimmed.Length + 8);
        var prevWasSep = false;
        for (var i = 0; i < trimmed.Length; i++) {
            var c = trimmed[i];
            if (c is '_' or ' ' || char.IsWhiteSpace(c)) {
                AppendHyphen(sb, ref prevWasSep);
                continue;
            }

            if (c == '-') {
                AppendHyphen(sb, ref prevWasSep);
                continue;
            }

            if (char.IsUpper(c)) {
                if (sb.Length > 0 && !prevWasSep && char.IsLetter(trimmed[i - 1]) && char.IsLower(trimmed[i - 1]))
                    AppendHyphen(sb, ref prevWasSep);

                sb.Append(char.ToLowerInvariant(c));
                prevWasSep = false;
                continue;
            }

            if (char.IsLetterOrDigit(c)) {
                sb.Append(char.ToLowerInvariant(c));
                prevWasSep = false;
                continue;
            }

            AppendHyphen(sb, ref prevWasSep);
        }

        var normalized = CollapseHyphens(sb.ToString());
        return string.IsNullOrEmpty(normalized) ? fallback : normalized;
    }

    private static void AppendHyphen(StringBuilder sb, ref bool prevWasSep)
    {
        if (sb.Length == 0 || sb[^1] == '-') {
            prevWasSep = true;
            return;
        }

        sb.Append('-');
        prevWasSep = true;
    }

    private static string CollapseHyphens(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        while (s.Contains("--", StringComparison.Ordinal))
            s = s.Replace("--", "-", StringComparison.Ordinal);

        return s.Trim('-');
    }
}
