using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Sanitization;

namespace Lyo.Reporting.Models.Composition;

/// <summary>Resolves sanitized CSS for <see cref="Layout" /> body padding and <c>@page</c> print margins.</summary>
public static class ReportLayoutCss
{
    /// <summary>Theme fallback padding when the layout does not set padding.</summary>
    public static string DefaultPadding(bool compact) => compact ? "24px" : "40px";

    /// <summary>Sanitized body padding: shorthand, then per-side overrides, then the theme default.</summary>
    public static string ResolvePadding(Layout layout, bool compact)
    {
        var shorthand = ReportHtmlSanitizer.SanitizeCss(layout.Padding);
        var top = FirstCss(layout.PaddingTop, shorthand);
        var right = FirstCss(layout.PaddingRight, shorthand);
        var bottom = FirstCss(layout.PaddingBottom, shorthand);
        var left = FirstCss(layout.PaddingLeft, shorthand);
        if (HasAny(layout.PaddingTop, layout.PaddingRight, layout.PaddingBottom, layout.PaddingLeft)) {
            var t = string.IsNullOrEmpty(top) ? DefaultPadding(compact) : top;
            var r = string.IsNullOrEmpty(right) ? t : right;
            var b = string.IsNullOrEmpty(bottom) ? t : bottom;
            var l = string.IsNullOrEmpty(left) ? r : left;
            return $"{t} {r} {b} {l}";
        }

        return string.IsNullOrEmpty(shorthand) ? DefaultPadding(compact) : shorthand;
    }

    /// <summary>
    /// <c>@page</c> rule for print/PDF, or empty when size and margin are all default-auto. Values are sanitized.
    /// </summary>
    public static string ResolvePageRule(Layout layout)
    {
        var size = ResolvePageSize(layout);
        var margin = ResolveMargin(layout);
        if (string.IsNullOrEmpty(size) && string.IsNullOrEmpty(margin))
            return string.Empty;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(size))
            parts.Add($"size: {size}");
        if (!string.IsNullOrEmpty(margin))
            parts.Add($"margin: {margin}");

        return "@page { " + string.Join("; ", parts) + "; }";
    }

    private static string ResolvePageSize(Layout layout)
    {
        var landscape = string.Equals(layout.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase);
        var size = layout.PageSize ?? "Auto";
        if (string.Equals(size, "A4", StringComparison.OrdinalIgnoreCase))
            return landscape ? "A4 landscape" : "A4 portrait";

        if (string.Equals(size, "Letter", StringComparison.OrdinalIgnoreCase))
            return landscape ? "letter landscape" : "letter portrait";

        return string.Empty;
    }

    private static string ResolveMargin(Layout layout)
    {
        var shorthand = ReportHtmlSanitizer.SanitizeCss(layout.Margin);
        if (HasAny(layout.MarginTop, layout.MarginRight, layout.MarginBottom, layout.MarginLeft)) {
            var t = FirstCss(layout.MarginTop, shorthand);
            var r = FirstCss(layout.MarginRight, shorthand);
            var b = FirstCss(layout.MarginBottom, shorthand);
            var l = FirstCss(layout.MarginLeft, shorthand);
            if (string.IsNullOrEmpty(t) && string.IsNullOrEmpty(r) && string.IsNullOrEmpty(b) && string.IsNullOrEmpty(l) && string.IsNullOrEmpty(shorthand))
                return string.Empty;

            var fallback = string.IsNullOrEmpty(shorthand) ? "0" : shorthand;
            return $"{Or(t, fallback)} {Or(r, fallback)} {Or(b, fallback)} {Or(l, fallback)}";
        }

        return shorthand;
    }

    private static string FirstCss(string? preferred, string fallback)
    {
        var sanitized = ReportHtmlSanitizer.SanitizeCss(preferred);
        return string.IsNullOrEmpty(sanitized) ? fallback : sanitized;
    }

    private static string Or(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;

    private static bool HasAny(params string?[] values) => values.Any(v => !string.IsNullOrWhiteSpace(v));
}
