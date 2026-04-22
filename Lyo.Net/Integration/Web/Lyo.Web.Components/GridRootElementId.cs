using System.Text;

namespace Lyo.Web.Components;

/// <summary>Stable grid root HTML <c>id</c> prefixes (<c>lyo-data-grid-…</c> / <c>lyo-data-grid-projected-…</c>).</summary>
internal static class GridRootElementId
{
    public static string DataGrid(string gridKey) => $"lyo-data-grid-{ToKebabSegment(gridKey)}";

    public static string DataGridProjected(string gridKey) => $"lyo-data-grid-projected-{ToKebabSegment(gridKey)}";

    private static string ToKebabSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "default";
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

        var s = CollapseHyphens(sb.ToString());
        return string.IsNullOrEmpty(s) ? "default" : s;
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
        if (string.IsNullOrEmpty(s)) return s;
        while (s.Contains("--", StringComparison.Ordinal))
            s = s.Replace("--", "-", StringComparison.Ordinal);
        return s.Trim('-');
    }
}
