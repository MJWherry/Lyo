using System.Linq;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Formatter;

namespace Lyo.Web.Automation.Plan;

/// <summary>
/// Replaces placeholders in step strings using <see cref="AutomationPlanInterpolationBindings" /> (live run context). When
/// <see cref="ExpandAsync(string, AutomationPlanInterpolationBindings, IFormatterService?, CancellationToken)" /> is given an <see cref="IFormatterService" />, the template is
/// validated with SmartFormat (same engine as <see cref="FormatterService" />) before values are resolved.
/// </summary>
public static class AutomationPlanInterpolation
{
    private static readonly Regex SimplePlaceholder = new(@"\{\{([a-zA-Z_][a-zA-Z0-9_]*)\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DoubleBracePlaceholder = new(@"\{\{\s*([^}]+?)\s*\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SingleBraceToken = new(@"\{(?<inner>[^{}]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Expands only simple <c>{{variableName}}</c> placeholders from string variables (legacy, synchronous).</summary>
    public static string Expand(string template, IReadOnlyDictionary<string, string> strings)
    {
        ArgumentHelpers.ThrowIfNull(template);
        ArgumentHelpers.ThrowIfNull(strings);
        return SimplePlaceholder.Replace(
            template, m => {
                var key = m.Groups[1].Value;
                var foundValue = strings.TryGetValue(key, out var value);
                OperationHelpers.ThrowIf(!foundValue, $"Template references undefined string variable '{{{{{key}}}}}'. Define it with a store or extract step first.");
                return value ?? string.Empty;
            });
    }

    /// <summary>Expands placeholders using strings, lists, elements, and optionally the browser.</summary>
    /// <remarks>
    /// <para><b>Syntax</b>: use <c>{{page.url}}</c> (legacy) or SmartFormat-style <c>{page.url}</c>. Double braces are normalized to single braces before parsing.</para>
    /// <para>Selector paths (after optional <see cref="IFormatterService" /> validation):</para>
    /// <list type="bullet">
    /// <item><c>name</c> — string variable (simple identifier only in legacy <see cref="Expand" />).</item> <item><c>strings.x</c> / <c>str.x</c> — string variable <c>x</c>.</item>
    /// <item><c>lists.x</c> / <c>list.x</c> — string-list <c>x</c>, lines joined with newlines.</item>
    /// <item><c>page.url</c>, <c>page.title</c> — current document (requires <see cref="AutomationPlanInterpolationBindings.Browser" />).</item>
    /// <item><c>elements.ref.text</c> or <c>el.ref.text</c> — visible text of element ref <c>ref</c>.</item> <item><c>elements.ref.attr.href</c> — DOM attribute.</item>
    /// </list>
    /// If the full selector matches a string variable key (including keys with dots), that value wins first. A SmartFormat format specifier after <c>:</c> is ignored for resolution; only
    /// the selector before <c>:</c> is used.
    /// </remarks>
    public static async Task<string> ExpandAsync(string template, AutomationPlanInterpolationBindings bindings, IFormatterService? formatter, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(template);
        ArgumentHelpers.ThrowIfNull(bindings);
        var work = template.IndexOf("{{", StringComparison.Ordinal) >= 0 ? DoubleBracePlaceholder.Replace(template, m => "{" + m.Groups[1].Value.Trim() + "}") : template;
        if (!SingleBraceToken.IsMatch(work))
            return work;

        if (formatter != null && !formatter.TryValidateTemplate(work, out var err))
            throw new InvalidOperationException($"Automation plan template is invalid: {err}");

        foreach (var m in SingleBraceToken.Matches(work).Cast<Match>().OrderByDescending(static x => x.Index)) {
            var inner = m.Groups["inner"].Value;
            var colon = inner.IndexOf(':');
            var selector = (colon >= 0 ? inner.Substring(0, colon) : inner).Trim();
            OperationHelpers.ThrowIf(selector.Length == 0, "Template contains an empty placeholder {}.");
            var resolved = await ResolvePlaceholderAsync(selector, bindings, ct).ConfigureAwait(false);
            work = work.Remove(m.Index, m.Length).Insert(m.Index, resolved);
        }

        return work;
    }

    /// <summary>Returns true if <paramref name="template" /> may contain placeholders (<c>{{</c> or <c>{…}</c>).</summary>
    public static bool ContainsPlaceholders(string template)
    {
        if (string.IsNullOrEmpty(template))
            return false;

        return template.IndexOf("{{", StringComparison.Ordinal) >= 0 || (template.IndexOf('{') >= 0 && SingleBraceToken.IsMatch(template));
    }

    private static async Task<string> ResolvePlaceholderAsync(string spec, AutomationPlanInterpolationBindings b, CancellationToken ct)
    {
        var s = spec.Trim();
        OperationHelpers.ThrowIf(s.Length == 0, "Template placeholder is empty.");
        if (b.Strings.TryGetValue(s, out var direct))
            return direct;

        var parts = s.Split('.').Select(static p => p.Trim()).Where(static p => p.Length > 0).ToArray();
        OperationHelpers.ThrowIf(
            parts.Length < 2, $"Template references unknown placeholder '{{{s}}}'. Use a string variable, or forms like strings.x, lists.x, page.url, el.ref.text.");

        var head = parts[0];
        if (head.Equals("strings", StringComparison.OrdinalIgnoreCase) || head.Equals("str", StringComparison.OrdinalIgnoreCase)) {
            OperationHelpers.ThrowIf(parts.Length != 2, $"Invalid strings placeholder '{{{s}}}'.");
            var key = parts[1];
            var foundStr = b.Strings.TryGetValue(key, out var v);
            OperationHelpers.ThrowIf(!foundStr, $"Unknown string variable '{key}'.");
            return v ?? string.Empty;
        }

        if (head.Equals("lists", StringComparison.OrdinalIgnoreCase) || head.Equals("list", StringComparison.OrdinalIgnoreCase)) {
            OperationHelpers.ThrowIf(parts.Length != 2, $"Invalid lists placeholder '{{{s}}}'.");
            var key = parts[1];
            var foundList = b.StringLists.TryGetValue(key, out var list);
            OperationHelpers.ThrowIf(!foundList || list == null, $"Unknown string list variable '{key}'.");
            return string.Join(Environment.NewLine, list!);
        }

        if (head.Equals("page", StringComparison.OrdinalIgnoreCase)) {
            OperationHelpers.ThrowIfNull(b.Browser, "page.url / page.title require an active browser in the interpolation bindings.");
            OperationHelpers.ThrowIf(parts.Length != 2, $"Invalid page placeholder '{{{s}}}'.");
            var p = parts[1];
            if (p.Equals("url", StringComparison.OrdinalIgnoreCase))
                return await b.Browser.GetCurrentUrlAsync(ct).ConfigureAwait(false);

            if (p.Equals("title", StringComparison.OrdinalIgnoreCase))
                return await b.Browser.GetTitleAsync(ct).ConfigureAwait(false);

            throw new InvalidOperationException($"Unknown page field '{p}'. Use url or title.");
        }

        if (head.Equals("elements", StringComparison.OrdinalIgnoreCase) || head.Equals("el", StringComparison.OrdinalIgnoreCase)) {
            OperationHelpers.ThrowIf(parts.Length < 3, $"Invalid element placeholder '{{{s}}}'. Expected el.ref.text or el.ref.attr.name.");
            var refName = parts[1];
            var foundEl = b.Elements.TryGetValue(refName, out var el);
            OperationHelpers.ThrowIf(!foundEl || el == null, $"Unknown element ref '{refName}'.");
            if (parts[2].Equals("text", StringComparison.OrdinalIgnoreCase)) {
                OperationHelpers.ThrowIf(parts.Length != 3, $"Invalid element text placeholder '{{{s}}}'.");
                return await el!.GetTextAsync(ct).ConfigureAwait(false);
            }

            if (parts[2].Equals("attr", StringComparison.OrdinalIgnoreCase)) {
                OperationHelpers.ThrowIf(parts.Length != 4, $"Invalid element attribute placeholder '{{{s}}}'. Use el.ref.attr.attributeName.");
                var attr = parts[3];
                return await el!.GetAttributeAsync(attr, ct).ConfigureAwait(false) ?? string.Empty;
            }

            throw new InvalidOperationException($"Unknown element field '{parts[2]}' in '{{{s}}}'. Use text or attr.");
        }

        throw new InvalidOperationException($"Template references unknown placeholder '{{{s}}}'. See AutomationPlanInterpolation.ExpandAsync remarks.");
    }
}