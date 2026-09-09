using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Response;

namespace Lyo.Reporting.Models.Composition;

/// <summary>
/// Binds generation or example parameter values into a report composition: interpolates <c>{Key}</c> placeholders and hides blocks whose
/// <c>VisibleWhen</c> condition fails. Call this before generate or file render so the viewer stays a dumb layout engine. The design canvas
/// interpolates at paint time on the live graph instead of cloning.
/// </summary>
public static class ReportCompositionProcessor
{
    private static readonly Regex Placeholder = new(@"\{([A-Za-z_][A-Za-z0-9_.]*)\}", RegexOptions.Compiled);

    /// <summary>Deep-clones <paramref name="report" /> via JSON, interpolates placeholders, and drops hidden sections and blocks.</summary>
    public static Report<T> Bind<T>(Report<T> report, IReadOnlyDictionary<string, string?> parameters)
    {
        ArgumentHelpers.ThrowIfNull(report);
        ArgumentHelpers.ThrowIfNull(parameters);
        var clone = ReportJson.Deserialize<T>(ReportJson.Serialize(report));
        Apply(clone, parameters);
        return clone;
    }

    /// <summary>Deserializes composition JSON, binds parameters, and returns the bound report.</summary>
    public static Report<object> BindJson(string json, IReadOnlyDictionary<string, string?> parameters)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        ArgumentHelpers.ThrowIfNull(parameters);
        var report = ReportJson.Deserialize<object>(json);
        Apply(report, parameters);
        return report;
    }

    /// <summary>Binds using generation-parameter rows (null or empty is treated as no values).</summary>
    public static Report<object> BindJson(string json, IEnumerable<ReportGenerationParameterRes>? parameters)
        => BindJson(json, ToMap(parameters));

    /// <summary>Builds a case-insensitive key/value map from generation parameters.</summary>
    public static IReadOnlyDictionary<string, string?> ToMap(IEnumerable<ReportGenerationParameterRes>? parameters)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (parameters is null)
            return map;

        foreach (var p in parameters) {
            if (string.IsNullOrWhiteSpace(p.Key))
                continue;

            map[p.Key] = UnwrapJsonValue(p.Value);
        }

        return map;
    }

    /// <summary>Builds a case-insensitive map from workbench example values, falling back to each spec's default then example.</summary>
    public static IReadOnlyDictionary<string, string?> ToMap(IEnumerable<ParameterSpec>? specs, IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (specs is not null) {
            foreach (var spec in specs) {
                if (string.IsNullOrWhiteSpace(spec.Key))
                    continue;

                var value = spec.ExampleValue ?? spec.DefaultValue;
                if (value is not null)
                    map[spec.Key] = UnwrapJsonValue(value);
            }
        }

        if (overrides is not null) {
            foreach (var kvp in overrides) {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    continue;

                map[kvp.Key] = UnwrapJsonValue(kvp.Value);
            }
        }

        return map;
    }

    /// <summary>True when <paramref name="visibleWhen" /> is empty or the parameter map satisfies the condition.</summary>
    public static bool IsVisible(string? visibleWhen, IReadOnlyDictionary<string, string?> parameters)
    {
        ArgumentHelpers.ThrowIfNull(parameters);
        if (string.IsNullOrWhiteSpace(visibleWhen))
            return true;

        var expr = visibleWhen!.Trim();
        if (expr[0] == '!')
            return string.IsNullOrWhiteSpace(Lookup(parameters, expr[1..].Trim()));

        var notEquals = expr.IndexOf("!=", StringComparison.Ordinal);
        if (notEquals > 0) {
            var key = expr[..notEquals].Trim();
            var expected = expr[(notEquals + 2)..].Trim();
            return !string.Equals(Lookup(parameters, key) ?? string.Empty, expected, StringComparison.OrdinalIgnoreCase);
        }

        var equals = expr.IndexOf('=');
        if (equals > 0) {
            var key = expr[..equals].Trim();
            var expected = expr[(equals + 1)..].Trim();
            return string.Equals(Lookup(parameters, key) ?? string.Empty, expected, StringComparison.OrdinalIgnoreCase);
        }

        return !string.IsNullOrWhiteSpace(Lookup(parameters, expr));
    }

    private static void Apply<T>(Report<T> report, IReadOnlyDictionary<string, string?> parameters)
    {
        report.Title = Interpolate(report.Title, parameters);
        report.Subtitle = Interpolate(report.Subtitle, parameters);
        report.Description = Interpolate(report.Description, parameters);
        report.Footer = Interpolate(report.Footer, parameters);
        if (report.Layout is not null) {
            report.Layout.HeaderText = Interpolate(report.Layout.HeaderText, parameters);
            report.Layout.Watermark = Interpolate(report.Layout.Watermark, parameters);
            report.Layout.LogoUrl = Interpolate(report.Layout.LogoUrl, parameters);
            report.Layout.AccentColor = Interpolate(report.Layout.AccentColor, parameters);
            report.Layout.RootComponentType = Interpolate(report.Layout.RootComponentType, parameters);
            report.Layout.Padding = Interpolate(report.Layout.Padding, parameters);
            report.Layout.Margin = Interpolate(report.Layout.Margin, parameters);
        }

        report.Sections = BindSections(report.Sections, parameters);
        FillTableOfContents(report.Sections);
    }

    private static List<Section> BindSections(List<Section> sections, IReadOnlyDictionary<string, string?> parameters)
    {
        var kept = new List<Section>(sections.Count);
        foreach (var section in sections.OrderBy(s => s.Order)) {
            if (!IsVisible(section.VisibleWhen, parameters))
                continue;

            section.Title = Interpolate(section.Title, parameters);
            section.Subtitle = Interpolate(section.Subtitle, parameters);
            section.Description = Interpolate(section.Description, parameters);
            section.Controls = BindControls(section.Controls, parameters);
            section.Subsections = BindSections(section.Subsections, parameters);
            kept.Add(section);
        }

        return kept;
    }

    private static List<Control> BindControls(List<Control> controls, IReadOnlyDictionary<string, string?> parameters)
    {
        var kept = new List<Control>(controls.Count);
        foreach (var control in controls) {
            switch (control) {
                case Card card:
                    card.Label = Interpolate(card.Label, parameters);
                    card.Value = InterpolateValue(card.Value, parameters);
                    kept.Add(card);
                    break;
                case Block block:
                    if (!IsVisible(block.VisibleWhen, parameters))
                        break;

                    BindBlock(block, parameters);
                    kept.Add(block);
                    break;
                case Table table:
                    BindTable(table, parameters);
                    kept.Add(table);
                    break;
                case Grid grid:
                    grid.Title = Interpolate(grid.Title, parameters);
                    grid.Gap = Interpolate(grid.Gap, parameters);
                    grid.TemplateColumns = Interpolate(grid.TemplateColumns, parameters);
                    grid.Controls = BindControls(grid.Controls.Where(c => c is not Grid).ToList(), parameters);
                    kept.Add(grid);
                    break;
                default:
                    kept.Add(control);
                    break;
            }
        }

        return kept;
    }

    private static void BindBlock(Block block, IReadOnlyDictionary<string, string?> parameters)
    {
        block.Content = Interpolate(block.Content, parameters);
        block.Caption = Interpolate(block.Caption, parameters);
        block.Source = Interpolate(block.Source, parameters);
        block.Alt = Interpolate(block.Alt, parameters);
        block.ComponentType = Interpolate(block.ComponentType, parameters);
        foreach (var binding in block.ParameterBindings.Values) {
            if (binding.Kind == ComponentBindingKind.Param) {
                binding.Value = Lookup(parameters, binding.Value ?? string.Empty) ?? binding.Value;
                binding.Kind = ComponentBindingKind.Literal;
            }
            else
                binding.Value = Interpolate(binding.Value, parameters);
        }

        if (block.ListItems is not null) {
            for (var i = 0; i < block.ListItems.Count; i++)
                block.ListItems[i] = Interpolate(block.ListItems[i], parameters) ?? string.Empty;
        }
    }

    private static void BindTable(Table table, IReadOnlyDictionary<string, string?> parameters)
    {
        table.Title = Interpolate(table.Title, parameters);
        table.Caption = Interpolate(table.Caption, parameters);
        foreach (var col in table.Columns)
            col.Header = Interpolate(col.Header, parameters) ?? string.Empty;

        foreach (var row in table.Rows) {
            for (var i = 0; i < row.Cells.Count; i++)
                row.Cells[i] = InterpolateValue(row.Cells[i], parameters);
        }
    }

    /// <summary>Replaces <c>{Key}</c> placeholders. Unknown keys are left unchanged. Null or empty text is returned as-is.</summary>
    public static string? Interpolate(string? text, IReadOnlyDictionary<string, string?> parameters)
    {
        ArgumentHelpers.ThrowIfNull(parameters);
        if (string.IsNullOrEmpty(text))
            return text;

        return Placeholder.Replace(
            text, match => {
                var key = match.Groups[1].Value;
                if (key.StartsWith("param.", StringComparison.OrdinalIgnoreCase))
                    key = key["param.".Length..];

                return Lookup(parameters, key) ?? match.Value;
            });
    }

    /// <summary>Interpolates string and JSON-string values; other values pass through unless their invariant string contains <c>{</c>.</summary>
    public static object? InterpolateValue(object? value, IReadOnlyDictionary<string, string?> parameters)
    {
        ArgumentHelpers.ThrowIfNull(parameters);
        if (value is null)
            return null;

        if (value is string s)
            return Interpolate(s, parameters);

        if (value is JsonElement element) {
            if (element.ValueKind == JsonValueKind.String)
                return Interpolate(element.GetString(), parameters);

            return element.ToString();
        }

        var asString = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (asString is not null && asString.IndexOf('{') >= 0)
            return Interpolate(asString, parameters);

        return value;
    }

    private static string? Lookup(IReadOnlyDictionary<string, string?> parameters, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return parameters.TryGetValue(key.Trim(), out var value) ? value : null;
    }

    /// <summary>Definition and generation values are stored as JSON; a quoted string should bind as the inner text so <c>{Key}</c> is not left with quotes.</summary>
    private static string? UnwrapJsonValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var trimmed = value!.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"') {
            try {
                return JsonSerializer.Deserialize<string>(trimmed) ?? trimmed;
            }
            catch (JsonException) {
                return trimmed;
            }
        }

        return trimmed;
    }

    private static void FillTableOfContents(IEnumerable<Section> sections)
    {
        var titles = CollectHeadings(sections).ToList();
        foreach (var section in SectionBody.WalkSections(sections)) {
            foreach (var block in SectionBody.WalkControls(section.Controls).OfType<Block>().Where(b => b.ContentType == ContentType.TableOfContents)) {
                if (block.ListItems is { Count: > 0 })
                    continue;

                block.ListItems = [..titles];
            }
        }
    }

    /// <summary>Section titles and heading blocks in document order. Bind copies these onto empty TOC blocks; the designer paints them without mutating.</summary>
    public static IEnumerable<string> CollectHeadings(IEnumerable<Section> sections)
    {
        foreach (var section in sections.OrderBy(s => s.Order)) {
            if (!string.IsNullOrWhiteSpace(section.Title))
                yield return section.Title!;

            foreach (var heading in SectionBody.WalkControls(section.Controls).OfType<Block>().Where(b => b.ContentType == ContentType.Heading && !string.IsNullOrWhiteSpace(b.Content)))
                yield return heading.Content!;

            foreach (var nested in CollectHeadings(section.Subsections))
                yield return nested;
        }
    }
}
