using System.Globalization;
using System.Net;
using System.Text.Json;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Sanitization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Lyo.Reporting.Web.Components;

/// <summary>
/// Shared paint for <see cref="ReportViewer{T}" /> and the design canvas. No drag handles or selection rings — callers wrap body items when they need designer chrome.
/// </summary>
public sealed class ReportViewPainter
{
    /// <summary>Shared paint for generate <see cref="ReportViewer{T}" /> and the design canvas.</summary>
    /// <param name="layout">Report layout (theme, page size, padding).</param>
    /// <param name="reportStyles">Optional CSS declarations on the report root.</param>
    /// <param name="previewParameters">When set, the designer interpolates <c>{Key}</c> at paint time without cloning the composition.</param>
    /// <param name="previewSections">Live sections for designer TOC autofill. Null on generate.</param>
    public ReportViewPainter(
        Layout layout,
        IReadOnlyDictionary<string, string>? reportStyles = null,
        IReadOnlyDictionary<string, string?>? previewParameters = null,
        IReadOnlyList<Section>? previewSections = null)
    {
        Layout = layout ?? new();
        ReportStyles = reportStyles ?? new Dictionary<string, string>();
        PreviewParameters = previewParameters;
        PreviewSections = previewSections;
    }

    public Layout Layout { get; }

    public IReadOnlyDictionary<string, string> ReportStyles { get; }

    /// <summary>Example parameter map for designer paint. Null on generate so authored text is written unchanged.</summary>
    public IReadOnlyDictionary<string, string?>? PreviewParameters { get; }

    /// <summary>Live section graph for designer-only paint (TOC headings). Null on generate.</summary>
    public IReadOnlyList<Section>? PreviewSections { get; }

    /// <summary>Interpolates <paramref name="text" /> when <see cref="PreviewParameters" /> is set; otherwise returns it unchanged.</summary>
    public string? P(string? text)
        => PreviewParameters is null ? text : ReportCompositionProcessor.Interpolate(text, PreviewParameters);

    /// <summary>Interpolates cell/card values when <see cref="PreviewParameters" /> is set.</summary>
    public object? Pv(object? value)
        => PreviewParameters is null ? value : ReportCompositionProcessor.InterpolateValue(value, PreviewParameters);

    private (string Label, string Detail) SplitP(string? item)
    {
        var text = P(item) ?? string.Empty;
        var split = text.IndexOf('|');
        return split < 0 ? (text, string.Empty) : (text[..split], text[(split + 1)..]);
    }

    public string AccentColor
    {
        get
        {
            var color = P(Layout.AccentColor);
            return string.IsNullOrWhiteSpace(color) ? "#2563eb" : color!;
        }
    }

    public bool IsCompact => string.Equals(Layout.Theme, "Compact", StringComparison.OrdinalIgnoreCase);

    public bool IsFormal => string.Equals(Layout.Theme, "Formal", StringComparison.OrdinalIgnoreCase);

    public string InkColor => IsFormal ? "#111827" : "#1e293b";

    public string BodyColor => IsFormal ? "#1f2937" : "#475569";

    public string MutedColor => "#64748b";

    public string FontFamily => IsFormal
        ? "Georgia, 'Times New Roman', serif"
        : "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

    public string? SafeLogoUrl
    {
        get
        {
            var logo = P(Layout.LogoUrl);
            if (string.IsNullOrWhiteSpace(logo))
                return null;

            var html = ReportHtmlSanitizer.Sanitize($"<img src=\"{WebUtility.HtmlEncode(logo)}\" alt=\"\" />");
            return html.Contains("src=", StringComparison.OrdinalIgnoreCase) ? logo : null;
        }
    }

    public string? PageRuleCss => ReportLayoutCss.ResolvePageRule(Layout);

    public string GetRootStyles()
    {
        var width = ResolveMaxWidth();
        var padding = ReportLayoutCss.ResolvePadding(Layout, IsCompact);
        var styles = new List<string> {
            $"max-width: {width}",
            "margin: 0 auto",
            $"padding: {padding}",
            $"font-family: {FontFamily}",
            "position: relative",
            "background: white"
        };

        if (string.Equals(Layout.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase))
            styles.Add("min-height: 600px");

        foreach (var kvp in ReportStyles)
            AppendSanitized(styles, kvp.Key, kvp.Value);

        return string.Join("; ", styles);
    }

    public string GetWatermarkStyles()
        => "position: absolute; inset: 20% 0 auto 0; text-align: center; font-size: 72px; font-weight: 700; letter-spacing: 0.2em; color: rgba(15, 23, 42, 0.06); transform: rotate(-18deg); pointer-events: none; user-select: none; z-index: 0;";

    public string GetSectionStyles(Section section, int depth)
    {
        var pad = IsCompact ? "12px" : "20px";
        var indent = depth == 0 ? "0" : $"{Math.Min(depth, 4) * 24}px";
        var styles = new List<string> {
            "margin-bottom: 40px",
            $"padding: {pad}",
            "background: white",
            "border-radius: 8px",
            "box-shadow: 0 1px 3px rgba(0,0,0,0.1)",
            "position: relative",
            "z-index: 1",
            $"margin-left: {indent}"
        };

        foreach (var kvp in section.Styles)
            AppendSanitized(styles, kvp.Key, kvp.Value);

        return string.Join("; ", styles);
    }

    public string GetCardStyles(Card column)
    {
        var styles = new List<string>();
        if (!string.IsNullOrEmpty(column.Width))
            styles.Add($"width: {column.Width}");

        if (!string.IsNullOrEmpty(column.Alignment))
            styles.Add($"text-align: {column.Alignment}");

        foreach (var kvp in column.Styles)
            AppendSanitized(styles, kvp.Key, kvp.Value);

        return styles.Count > 0 ? string.Join("; ", styles) : string.Empty;
    }

    public string GetContentBlockStyles(Block block)
    {
        var styles = new List<string> { "margin: 15px 0;" };
        if (ReportKeepTogether.Effective(block))
            styles.Add(ReportKeepTogether.Css);

        foreach (var kvp in block.Styles)
            AppendSanitized(styles, kvp.Key, kvp.Value);

        return string.Join("; ", styles);
    }

    /// <summary>CSS grid wrapper. Keep-together defaults on so a dashboard row does not split across pages.</summary>
    public string GetGridWrapperStyles(Grid grid)
    {
        var resolved = grid.ResolveTemplateColumns();
        var template = ReportHtmlSanitizer.SanitizeCss(resolved);
        if (string.IsNullOrWhiteSpace(template))
            template = $"repeat({Math.Max(grid.ColumnCount, 1)}, 1fr)";

        var gap = string.IsNullOrWhiteSpace(grid.Gap) ? "16px" : ReportHtmlSanitizer.SanitizeCss(grid.Gap) ?? "16px";
        var styles = new List<string> {
            "display: grid",
            $"grid-template-columns: {template}",
            $"gap: {gap}",
            "margin: 20px 0"
        };
        if (ReportKeepTogether.Effective(grid))
            styles.Add(ReportKeepTogether.Css);

        foreach (var kvp in grid.Styles)
            AppendSanitized(styles, kvp.Key, kvp.Value);

        return string.Join("; ", styles);
    }

    /// <summary>Outer table wrapper (title + HTML table). Keep-together only when the table opts in.</summary>
    public string GetTableWrapperStyles(Table table)
    {
        var styles = new List<string> { "margin: 20px 0;" };
        if (ReportKeepTogether.Effective(table))
            styles.Add(ReportKeepTogether.Css);

        return string.Join(" ", styles);
    }

    public string SelectionRing(bool selected)
        => selected ? $"outline: 2px solid {AccentColor}; outline-offset: 2px;" : string.Empty;

    public RenderFragment RenderSection(Section section, int depth)
        => b => WriteSection(b, section, depth);

    /// <summary>Section title/subtitle/description only — the design canvas wraps this with hover chrome.</summary>
    public RenderFragment RenderSectionChrome(Section section, int depth)
        => b => WriteSectionChrome(b, section, depth);

    public void WriteSection(RenderTreeBuilder b, Section section, int depth)
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "style", GetSectionStyles(section, depth));
        if (section.Collapsed)
            b.AddAttribute(2, "hidden");

        WriteSectionChrome(b, section, depth);
        foreach (var bodyItem in SectionBody.Enumerate(section)) {
            switch (bodyItem.Kind) {
                case SectionBodyKind.Control when bodyItem.Control is not null:
                    WriteControl(b, bodyItem.Control);
                    break;
                case SectionBodyKind.Subsection when bodyItem.Subsection is not null:
                    WriteSection(b, bodyItem.Subsection, depth + 1);
                    break;
            }
        }

        b.CloseElement();
    }

    public void WriteSectionChrome(RenderTreeBuilder b, Section section, int depth)
    {
        var headingTag = depth == 0 ? "h2" : "h3";
        var headingSize = depth == 0 ? "24px" : "20px";
        if (!string.IsNullOrEmpty(section.Title)) {
            b.OpenElement(3, headingTag);
            b.AddAttribute(4, "style", $"color: {InkColor}; font-size: {headingSize}; font-weight: 600; margin-bottom: 20px; padding-bottom: 10px; border-bottom: 2px solid #e2e8f0;");
            b.AddContent(5, P(section.Title));
            b.CloseElement();
        }

        if (!string.IsNullOrEmpty(section.Subtitle)) {
            b.OpenElement(6, "h3");
            b.AddAttribute(7, "style", $"color: {MutedColor}; font-size: 18px; font-weight: 500; margin-bottom: 15px;");
            b.AddContent(8, P(section.Subtitle));
            b.CloseElement();
        }

        if (!string.IsNullOrEmpty(section.Description)) {
            b.OpenElement(9, "p");
            b.AddAttribute(10, "style", $"color: {BodyColor}; margin-bottom: 15px; line-height: 1.6;");
            b.AddContent(11, P(section.Description));
            b.CloseElement();
        }
    }

    public void WriteBodyItem(RenderTreeBuilder b, SectionBodyItem item)
    {
        if (item.Kind == SectionBodyKind.Control && item.Control is not null)
            WriteControl(b, item.Control);
    }

    public void WriteControl(RenderTreeBuilder b, Control control)
    {
        switch (control) {
            case Card card:
                WriteCard(b, card);
                break;
            case Block block:
                WriteContentBlock(b, block);
                break;
            case Table table:
                WriteTable(b, table);
                break;
            case Grid grid:
                WriteLayoutGrid(b, grid);
                break;
        }
    }

    public void WriteLayoutGrid(RenderTreeBuilder b, Grid grid, Action<RenderTreeBuilder, Control, int>? decorateChild = null)
    {
        if (!string.IsNullOrEmpty(grid.Title)) {
            b.OpenElement(2, "h3");
            b.AddAttribute(3, "style", $"color: {InkColor}; font-size: 18px; font-weight: 600; margin-bottom: 10px;");
            b.AddContent(4, P(grid.Title));
            b.CloseElement();
        }

        b.OpenElement(12, "div");
        b.AddAttribute(13, "style", GetGridWrapperStyles(grid));
        var index = 0;
        foreach (var child in grid.Controls) {
            var captured = child;
            b.OpenElement(14, "div");
            var extra = ChildSpanStyle(captured);
            if (!string.IsNullOrEmpty(extra))
                b.AddAttribute(15, "style", extra);

            if (decorateChild is null)
                WriteControl(b, captured);
            else
                decorateChild(b, captured, index);

            b.CloseElement();
            index++;
        }

        b.CloseElement();
    }

    private static string ChildSpanStyle(Control control)
    {
        var parts = new List<string> { "min-width: 0" };
        if (control.ColumnSpan > 1)
            parts.Add($"grid-column: span {control.ColumnSpan}");

        if (control.RowSpan > 1)
            parts.Add($"grid-row: span {control.RowSpan}");

        return string.Join("; ", parts);
    }

    /// <summary>Paints one KPI card. Designer callers pass <paramref name="decorate" /> so hit-testing and drag handles sit on this wrapper, not a nested card.</summary>
    /// <param name="extraStyle">Optional declarations appended to the card wrapper (designer selection ring).</param>
    /// <param name="decorate">Optional attributes/children on the card wrapper itself so the designer does not nest a second card.</param>
    public void WriteCard(RenderTreeBuilder b, Card column, string? extraStyle = null, Action<RenderTreeBuilder>? decorate = null)
    {
        b.OpenElement(14, "div");
        var style = GetCardStyles(column);
        if (!string.IsNullOrEmpty(extraStyle))
            style = string.IsNullOrEmpty(style) ? extraStyle : style + "; " + extraStyle;

        if (!string.IsNullOrEmpty(style))
            b.AddAttribute(15, "style", style);

        decorate?.Invoke(b);

        if (!string.IsNullOrEmpty(column.Label)) {
            b.OpenElement(40, "div");
            b.AddAttribute(41, "style", "font-size: 12px; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 6px;");
            b.AddContent(42, P(column.Label));
            b.CloseElement();
        }

        b.OpenElement(43, "div");
        b.AddAttribute(44, "style", column.Emphasized ? $"font-size: 18px; font-weight: 600; color: {InkColor};" : $"font-size: 16px; color: {BodyColor};");
        b.AddContent(45, FormatCell(Pv(column.Value), column.ValueFormatter));
        b.CloseElement();
        b.CloseElement();
    }

    public void WriteContentBlock(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "style", GetContentBlockStyles(block));
        switch (block.ContentType) {
            case ContentType.Text:
                b.OpenElement(2, "p");
                b.AddAttribute(3, "style", $"margin: 10px 0; line-height: 1.6; color: {BodyColor};");
                b.AddContent(4, P(block.Content));
                b.CloseElement();
                break;
            case ContentType.Html:
                b.AddMarkupContent(5, ReportHtmlSanitizer.Sanitize(P(block.Content)));
                break;
            case ContentType.List:
                WriteList(b, "ul", block);
                break;
            case ContentType.NumberedList:
                WriteList(b, "ol", block);
                break;
            case ContentType.Code:
                b.OpenElement(16, "pre");
                b.AddAttribute(17, "style", "background: #f1f5f9; padding: 15px; border-radius: 4px; overflow-x: auto; margin: 10px 0;");
                b.OpenElement(18, "code");
                b.AddContent(19, P(block.Content));
                b.CloseElement();
                b.CloseElement();
                break;
            case ContentType.Quote:
                b.OpenElement(20, "blockquote");
                b.AddAttribute(21, "style", $"border-left: 4px solid {AccentColor}; padding-left: 20px; margin: 10px 0; font-style: italic; color: {MutedColor};");
                b.AddContent(22, P(block.Content));
                b.CloseElement();
                break;
            case ContentType.Chart:
                WriteChart(b, block);
                break;
            case ContentType.Image:
                WriteImage(b, block);
                break;
            case ContentType.Divider:
                b.OpenElement(30, "hr");
                b.AddAttribute(31, "style", "border: 0; border-top: 1px solid #e2e8f0; margin: 16px 0;");
                b.CloseElement();
                break;
            case ContentType.Callout:
                WriteCallout(b, block);
                break;
            case ContentType.PageBreak:
                b.OpenElement(40, "div");
                b.AddAttribute(41, "style", PreviewParameters is null
                    ? "break-after: page; page-break-after: always; height: 0;"
                    : "break-after: page; page-break-after: always; margin: 16px 0; border-top: 2px dashed #94a3b8; color: #94a3b8; font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; text-align: center;");
                if (PreviewParameters is not null)
                    b.AddContent(411, "Page break");
                b.CloseElement();
                break;
            case ContentType.Spacer:
                b.OpenElement(42, "div");
                b.AddAttribute(43, "style", $"height: {block.Level.GetValueOrDefault(24)}px;");
                b.CloseElement();
                break;
            case ContentType.Progress:
                WriteProgress(b, block);
                break;
            case ContentType.Heading:
                WriteHeading(b, block);
                break;
            case ContentType.KeyValue:
                WriteKeyValue(b, block);
                break;
            case ContentType.Component:
                WriteComponent(b, block);
                break;
            case ContentType.Badge:
                WriteBadge(b, block);
                break;
            case ContentType.Signature:
                WriteSignature(b, block);
                break;
            case ContentType.TableOfContents:
                WriteTableOfContents(b, block);
                break;
            case ContentType.Timeline:
                WriteTimeline(b, block);
                break;
            case ContentType.Address:
                WriteAddress(b, block);
                break;
            case ContentType.Totals:
                WriteTotals(b, block);
                break;
            case ContentType.Checkbox:
                WriteCheckbox(b, block);
                break;
            case ContentType.Notes:
                WriteNotes(b, block);
                break;
        }

        b.CloseElement();
    }

    public void WriteChart(RenderTreeBuilder b, Block block)
    {
        if (block.ChartKind is not null) {
            var bound = PreviewParameters is null
                ? block
                : new Block {
                    ContentType = block.ContentType,
                    ChartKind = block.ChartKind,
                    Caption = P(block.Caption),
                    Content = P(block.Content),
                    Level = block.Level,
                    DataSourceKind = block.DataSourceKind,
                    DataParameterKey = P(block.DataParameterKey),
                    ChartLabelField = block.ChartLabelField,
                    ChartValueField = block.ChartValueField,
                    ListItems = PreviewChartSeries(block)
                };
            var html = ReportChartMarkup.TryBuild(bound, ReportChartMarkup.StableCanvasId(block));
            if (html is null) {
                b.OpenElement(23, "p");
                b.AddAttribute(24, "style", $"color: {MutedColor}; font-size: 13px;");
                b.AddContent(25, "Chart could not be drawn.");
                b.CloseElement();
                return;
            }

            b.AddMarkupContent(23, ReportHtmlSanitizer.Sanitize(html));
            return;
        }

        b.AddMarkupContent(23, ReportHtmlSanitizer.Sanitize(P(block.Content)));
    }

    private List<string>? PreviewChartSeries(Block block)
    {
        if (block.DataSourceKind == DataSourceKind.FromParameter && PreviewParameters is not null) {
            var key = P(block.DataParameterKey);
            PreviewParameters.TryGetValue(key ?? string.Empty, out var json);
            return ReportChartMarkup.SeriesFromRows(block, TableDataBinder.ParseRows(json));
        }

        return block.ListItems?.Select(i => P(i) ?? string.Empty).ToList();
    }

    public void WriteList(RenderTreeBuilder b, string tag, Block block)
        => WriteList(b, tag, block.ListItems);

    public void WriteImage(RenderTreeBuilder b, Block block)
    {
        var src = P(block.Source) ?? P(block.Content);
        if (string.IsNullOrWhiteSpace(src))
            return;

        var alt = WebUtility.HtmlEncode(P(block.Alt) ?? string.Empty);
        var encodedSrc = WebUtility.HtmlEncode(src);
        var html = $"<figure style=\"margin: 12px 0;\"><img src=\"{encodedSrc}\" alt=\"{alt}\" style=\"max-width: 100%; height: auto;\" />";
        var caption = P(block.Caption);
        if (!string.IsNullOrWhiteSpace(caption))
            html += $"<figcaption style=\"margin-top: 6px; font-size: 13px; color: {MutedColor};\">{WebUtility.HtmlEncode(caption)}</figcaption>";

        html += "</figure>";
        b.AddMarkupContent(32, ReportHtmlSanitizer.Sanitize(html));
    }

    public void WriteCallout(RenderTreeBuilder b, Block block)
    {
        var (bg, border, fg) = ResolveTone(block.Tone);
        b.OpenElement(33, "div");
        b.AddAttribute(34, "style", $"padding: 12px 16px; border-radius: 6px; background: {bg}; border-left: 4px solid {border}; color: {fg};");
        b.AddContent(35, P(block.Content));
        b.CloseElement();
    }

    public void WriteBadge(RenderTreeBuilder b, Block block)
    {
        var (bg, border, fg) = ResolveTone(block.Tone);
        b.OpenElement(70, "span");
        b.AddAttribute(71, "style", $"display: inline-block; padding: 2px 10px; border-radius: 999px; background: {bg}; border: 1px solid {border}; color: {fg}; font-size: 12px; font-weight: 600; letter-spacing: 0.04em; text-transform: uppercase;");
        b.AddContent(72, P(block.Content));
        b.CloseElement();
    }

    public void WriteSignature(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(73, "div");
        b.AddAttribute(74, "style", "margin: 24px 0 8px; max-width: 280px;");
        if (!string.IsNullOrWhiteSpace(block.Caption)) {
            b.OpenElement(75, "div");
            b.AddAttribute(76, "style", $"font-size: 12px; font-weight: 600; color: {MutedColor}; margin-bottom: 28px; text-transform: uppercase; letter-spacing: 0.04em;");
            b.AddContent(77, P(block.Caption));
            b.CloseElement();
        }

        b.OpenElement(78, "div");
        b.AddAttribute(79, "style", "border-bottom: 1px solid #334155; min-height: 36px;");
        b.AddContent(80, P(block.Content));
        b.CloseElement();
        b.OpenElement(81, "div");
        b.AddAttribute(82, "style", $"margin-top: 16px; border-bottom: 1px solid #334155; min-height: 28px; font-size: 13px; color: {MutedColor};");
        b.AddContent(83, string.IsNullOrWhiteSpace(block.Source) ? "Date" : P(block.Source));
        b.CloseElement();
        b.CloseElement();
    }

    public void WriteTableOfContents(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(84, "nav");
        b.AddAttribute(85, "style", "margin: 12px 0;");
        if (!string.IsNullOrWhiteSpace(block.Caption)) {
            b.OpenElement(86, "div");
            b.AddAttribute(87, "style", $"font-size: 13px; font-weight: 600; color: {InkColor}; margin-bottom: 8px;");
            b.AddContent(88, P(block.Caption));
            b.CloseElement();
        }

        WriteList(b, "ol", TocItems(block));
        b.CloseElement();
    }

    private IEnumerable<string> TocItems(Block block)
    {
        if (block.ListItems is { Count: > 0 })
            return block.ListItems;

        if (PreviewSections is null)
            return [];

        return ReportCompositionProcessor.CollectHeadings(PreviewSections).Select(t => P(t) ?? t);
    }

    public void WriteList(RenderTreeBuilder b, string tag, IEnumerable<string>? items)
    {
        b.OpenElement(6, tag);
        b.AddAttribute(7, "style", $"margin: 10px 0; padding-left: 25px; color: {BodyColor};");
        if (items != null) {
            foreach (var item in items) {
                b.OpenElement(8, "li");
                b.AddAttribute(9, "style", "margin-bottom: 8px;");
                b.AddContent(10, P(item));
                b.CloseElement();
            }
        }

        b.CloseElement();
    }

    public void WriteTimeline(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(89, "ol");
        b.AddAttribute(90, "style", "list-style: none; margin: 12px 0; padding: 0; border-left: 2px solid #e2e8f0;");
        if (block.ListItems != null) {
            foreach (var item in block.ListItems) {
                var (label, detail) = SplitP(item);
                b.OpenElement(91, "li");
                b.AddAttribute(92, "style", "position: relative; padding: 0 0 16px 20px;");
                b.OpenElement(93, "span");
                b.AddAttribute(94, "style", $"position: absolute; left: -6px; top: 4px; width: 10px; height: 10px; border-radius: 50%; background: {AccentColor};");
                b.CloseElement();
                b.OpenElement(95, "div");
                b.AddAttribute(96, "style", $"font-weight: 600; color: {InkColor};");
                b.AddContent(97, label);
                b.CloseElement();
                if (!string.IsNullOrEmpty(detail)) {
                    b.OpenElement(98, "div");
                    b.AddAttribute(99, "style", $"font-size: 13px; color: {MutedColor};");
                    b.AddContent(100, detail);
                    b.CloseElement();
                }

                b.CloseElement();
            }
        }

        b.CloseElement();
    }

    /// <summary>Stacked address: caption (Bill to / Ship to) plus <see cref="Block.ListItems" /> as lines.</summary>
    public void WriteAddress(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(101, "div");
        b.AddAttribute(102, "style", "margin: 8px 0; max-width: 320px;");
        if (!string.IsNullOrWhiteSpace(block.Caption)) {
            b.OpenElement(103, "div");
            b.AddAttribute(104, "style", $"font-size: 11px; font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase; color: {MutedColor}; margin-bottom: 6px;");
            b.AddContent(105, P(block.Caption));
            b.CloseElement();
        }

        if (block.ListItems != null) {
            foreach (var line in block.ListItems) {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                b.OpenElement(106, "div");
                b.AddAttribute(107, "style", $"line-height: 1.45; color: {BodyColor};");
                b.AddContent(108, P(line));
                b.CloseElement();
            }
        }

        b.CloseElement();
    }

    /// <summary>Totals stack from <c>label|amount</c> rows. The last row is heavier with a top border.</summary>
    public void WriteTotals(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(109, "div");
        b.AddAttribute(110, "style", "margin: 12px 0 8px auto; max-width: 280px;");
        var items = block.ListItems ?? [];
        for (var i = 0; i < items.Count; i++) {
            var (label, amount) = SplitP(items[i]);
            var last = i == items.Count - 1;
            b.OpenElement(111, "div");
            var rowStyle = last
                ? $"display: flex; justify-content: space-between; gap: 16px; padding: 8px 0 0; margin-top: 6px; border-top: 1px solid #334155; font-weight: 700; color: {InkColor};"
                : $"display: flex; justify-content: space-between; gap: 16px; padding: 4px 0; color: {BodyColor};";
            b.AddAttribute(112, "style", rowStyle);
            b.OpenElement(113, "span");
            b.AddContent(114, label);
            b.CloseElement();
            b.OpenElement(115, "span");
            b.AddContent(116, amount);
            b.CloseElement();
            b.CloseElement();
        }

        b.CloseElement();
    }

    /// <summary>Print-friendly empty squares plus term lines. Not interactive inputs.</summary>
    public void WriteCheckbox(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(117, "div");
        b.AddAttribute(118, "style", "margin: 10px 0;");
        if (!string.IsNullOrWhiteSpace(block.Content)) {
            b.OpenElement(119, "p");
            b.AddAttribute(120, "style", $"margin: 0 0 8px; color: {BodyColor};");
            b.AddContent(121, P(block.Content));
            b.CloseElement();
        }

        if (block.ListItems != null) {
            foreach (var term in block.ListItems) {
                b.OpenElement(122, "div");
                b.AddAttribute(123, "style", "display: flex; align-items: flex-start; gap: 8px; margin: 6px 0;");
                b.OpenElement(124, "span");
                b.AddAttribute(125, "style", "display: inline-block; width: 12px; height: 12px; margin-top: 3px; border: 1px solid #334155; border-radius: 2px; flex: 0 0 auto;");
                b.CloseElement();
                b.OpenElement(126, "span");
                b.AddAttribute(127, "style", $"color: {BodyColor}; line-height: 1.45;");
                b.AddContent(128, P(term));
                b.CloseElement();
                b.CloseElement();
            }
        }

        b.CloseElement();
    }

    /// <summary>Muted notes box. Caption plus multiline content — not a callout or quote.</summary>
    public void WriteNotes(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(129, "div");
        b.AddAttribute(130, "style", "margin: 12px 0; padding: 12px 14px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px;");
        if (!string.IsNullOrWhiteSpace(block.Caption)) {
            b.OpenElement(131, "div");
            b.AddAttribute(132, "style", $"font-size: 12px; font-weight: 600; color: {MutedColor}; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.04em;");
            b.AddContent(133, P(block.Caption));
            b.CloseElement();
        }

        b.OpenElement(134, "div");
        b.AddAttribute(135, "style", $"color: {BodyColor}; line-height: 1.55; white-space: pre-wrap;");
        b.AddContent(136, P(block.Content));
        b.CloseElement();
        b.CloseElement();
    }

    public (string Bg, string Border, string Fg) ResolveTone(string? tone)
        => (tone ?? "info").Trim().ToLowerInvariant() switch {
            "success" => ("#ecfdf5", "#059669", "#065f46"),
            "warning" => ("#fffbeb", "#d97706", "#92400e"),
            "error" => ("#fef2f2", "#dc2626", "#991b1b"),
            _ => ("#eff6ff", AccentColor, "#1e3a8a")
        };

    public void WriteProgress(RenderTreeBuilder b, Block block)
    {
        var percent = block.Level.GetValueOrDefault(0);
        if (percent < 0)
            percent = 0;
        else if (percent > 100)
            percent = 100;

        if (!string.IsNullOrWhiteSpace(block.Caption) || !string.IsNullOrWhiteSpace(block.Content)) {
            b.OpenElement(36, "div");
            b.AddAttribute(37, "style", $"font-size: 13px; font-weight: 600; color: {MutedColor}; margin-bottom: 6px;");
            b.AddContent(38, P(block.Caption) ?? P(block.Content));
            b.AddContent(39, $" ({percent}%)");
            b.CloseElement();
        }

        b.OpenElement(44, "div");
        b.AddAttribute(45, "style", "height: 10px; background: #e2e8f0; border-radius: 999px; overflow: hidden;");
        b.OpenElement(46, "div");
        b.AddAttribute(47, "style", $"width: {percent.ToString(CultureInfo.InvariantCulture)}%; height: 100%; background: {AccentColor};");
        b.CloseElement();
        b.CloseElement();
    }

    public void WriteHeading(RenderTreeBuilder b, Block block)
    {
        var level = block.Level.GetValueOrDefault(3);
        if (level < 1)
            level = 1;
        else if (level > 6)
            level = 6;

        var size = level switch {
            1 => "28px",
            2 => "24px",
            3 => "20px",
            4 => "18px",
            _ => "16px"
        };

        b.OpenElement(48, "h" + level);
        b.AddAttribute(49, "style", $"margin: 16px 0 8px; font-size: {size}; font-weight: 600; color: {InkColor};");
        b.AddContent(50, P(block.Content));
        b.CloseElement();
    }

    public void WriteKeyValue(RenderTreeBuilder b, Block block)
    {
        b.OpenElement(51, "dl");
        b.AddAttribute(52, "style", "display: grid; grid-template-columns: minmax(120px, 30%) 1fr; gap: 8px 16px; margin: 10px 0;");
        if (block.ListItems != null) {
            foreach (var item in block.ListItems) {
                var (label, value) = SplitP(item);
                b.OpenElement(53, "dt");
                b.AddAttribute(54, "style", $"font-size: 12px; font-weight: 600; color: {MutedColor}; text-transform: uppercase; letter-spacing: 0.4px;");
                b.AddContent(55, label);
                b.CloseElement();
                b.OpenElement(56, "dd");
                b.AddAttribute(57, "style", $"margin: 0; color: {BodyColor};");
                b.AddContent(58, value);
                b.CloseElement();
            }
        }

        b.CloseElement();
    }

    public void WriteComponent(RenderTreeBuilder b, Block block)
    {
        var type = ReportEmbedBinder.TryResolveComponent(P(block.ComponentType), out var error);
        if (type is null) {
            b.OpenElement(60, "p");
            b.AddAttribute(61, "style", $"color: {MutedColor}; font-size: 13px;");
            b.AddContent(62, error ?? "Unknown component.");
            b.CloseElement();
            return;
        }

        var parameters = ReportEmbedBinder.BindBlock(block, PreviewParameters, out var bindError);
        if (bindError is not null) {
            b.OpenElement(63, "p");
            b.AddAttribute(64, "style", $"color: {MutedColor}; font-size: 13px;");
            b.AddContent(65, bindError);
            b.CloseElement();
            return;
        }

        b.OpenComponent<DynamicComponent>(66);
        b.AddComponentParameter(67, "Type", type);
        if (parameters is { Count: > 0 })
            b.AddComponentParameter(68, "Parameters", parameters);

        b.CloseComponent();
    }

    public void WriteTable(RenderTreeBuilder b, Table grid, Action<RenderTreeBuilder, TableColumn, int>? decorateHeader = null)
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "style", GetTableWrapperStyles(grid));
        if (!string.IsNullOrEmpty(grid.Title)) {
            b.OpenElement(2, "h3");
            b.AddAttribute(3, "style", $"color: {InkColor}; font-size: 18px; font-weight: 600; margin-bottom: 10px;");
            b.AddContent(4, P(grid.Title));
            b.CloseElement();
        }

        if (!string.IsNullOrEmpty(grid.Caption)) {
            b.OpenElement(5, "p");
            b.AddAttribute(6, "style", $"color: {MutedColor}; font-size: 14px; margin-bottom: 10px;");
            b.AddContent(7, P(grid.Caption));
            b.CloseElement();
        }

        b.OpenElement(8, "table");
        b.AddAttribute(9, "style", GetTableStyles(grid));
        if (grid.ShowHeaders && (grid.Columns.Count > 0 || grid.ShowRowNumbers)) {
            b.OpenElement(10, "thead");
            b.OpenElement(11, "tr");
            b.AddAttribute(12, "style", "background: #f8fafc; border-bottom: 2px solid #e2e8f0;");
            if (grid.ShowRowNumbers) {
                b.OpenElement(13, "th");
                b.AddAttribute(14, "style", GetTableColumnHeaderStyles(null) + " width: 48px;");
                b.AddContent(15, "#");
                b.CloseElement();
            }

            var colIndex = 0;
            foreach (var col in grid.Columns) {
                b.OpenElement(13, "th");
                b.AddAttribute(14, "style", GetTableColumnHeaderStyles(col) + (decorateHeader is null ? string.Empty : " position: relative;"));
                if (decorateHeader is null)
                    b.AddContent(15, P(col.Header));
                else
                    decorateHeader(b, col, colIndex);

                b.CloseElement();
                colIndex++;
            }

            b.CloseElement();
            b.CloseElement();
        }

        b.OpenElement(16, "tbody");
        var rowIndex = 0;
        foreach (var row in PreviewTableRows(grid)) {
            b.OpenElement(17, "tr");
            b.AddAttribute(18, "style", GetTableRowStyles(grid, row, rowIndex));
            if (grid.ShowRowNumbers) {
                b.OpenElement(19, "td");
                b.AddAttribute(20, "style", "padding: 12px; color: #94a3b8; font-size: 12px;");
                b.AddContent(21, (rowIndex + 1).ToString(CultureInfo.InvariantCulture));
                b.CloseElement();
            }

            var cellIndex = 0;
            foreach (var cell in row.Cells) {
                b.OpenElement(19, "td");
                var column = cellIndex < grid.Columns.Count ? grid.Columns[cellIndex] : null;
                b.AddAttribute(20, "style", GetTableCellStyles(column));
                b.AddContent(21, FormatCell(Pv(cell), column?.ValueFormatter));
                b.CloseElement();
                cellIndex++;
            }

            b.CloseElement();
            rowIndex++;
        }

        b.CloseElement();
        b.CloseElement();
        b.CloseElement();
    }

    public static string FormatCell(object? value, Func<object?, string>? formatter)
    {
        if (formatter != null)
            return formatter(value);

        if (value is JsonElement element)
            return element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.ToString();

        return value?.ToString() ?? string.Empty;
    }

    public string GetTableStyles(Table grid)
    {
        var styles = new List<string> { "width: 100%;", "border-collapse: collapse;", "margin-top: 15px;" };
        if (grid.Bordered)
            styles.Add("border: 1px solid #e2e8f0;");

        foreach (var kvp in grid.Styles)
            AppendSanitized(styles, kvp.Key, kvp.Value);

        return string.Join(" ", styles);
    }

    public string GetTableColumnHeaderStyles(TableColumn? column)
    {
        var styles = new List<string> {
            "padding: 12px;",
            "text-align: left;",
            "font-size: 12px;",
            "font-weight: 600;",
            "color: #64748b;",
            "text-transform: uppercase;",
            "letter-spacing: 0.5px;"
        };

        if (column != null) {
            if (!string.IsNullOrEmpty(column.Alignment))
                styles.Add($"text-align: {column.Alignment};");

            if (!string.IsNullOrEmpty(column.Width))
                styles.Add($"width: {column.Width};");

            foreach (var kvp in column.Styles)
                AppendSanitized(styles, kvp.Key, kvp.Value);
        }

        return string.Join(" ", styles);
    }

    public string GetTableRowStyles(Table grid, TableRow row, int rowIndex)
    {
        var styles = new List<string> { "border-bottom: 1px solid #e2e8f0;" };
        if (grid.Striped && rowIndex % 2 == 1)
            styles.Add("background: #f8fafc;");

        if (row.Emphasized)
            styles.Add("background: #eff6ff; font-weight: 600;");

        foreach (var kvp in row.Styles)
            AppendSanitized(styles, kvp.Key, kvp.Value);

        return string.Join(" ", styles);
    }

    public string GetTableCellStyles(TableColumn? column)
    {
        var styles = new List<string> { "padding: 12px;", $"color: {BodyColor};" };
        if (column != null) {
            if (!string.IsNullOrEmpty(column.Alignment))
                styles.Add($"text-align: {column.Alignment};");

            foreach (var kvp in column.Styles)
                AppendSanitized(styles, kvp.Key, kvp.Value);
        }

        return string.Join(" ", styles);
    }

    public string ResolveMaxWidth()
    {
        var landscape = string.Equals(Layout.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase);
        var size = Layout.PageSize ?? "Auto";
        if (string.Equals(size, "A4", StringComparison.OrdinalIgnoreCase))
            return landscape ? "1123px" : "794px";

        if (string.Equals(size, "Letter", StringComparison.OrdinalIgnoreCase))
            return landscape ? "1056px" : "816px";

        return "1200px";
    }

    private IReadOnlyList<TableRow> PreviewTableRows(Table grid)
    {
        if (PreviewParameters is null || grid.DataSourceKind != DataSourceKind.FromParameter)
            return grid.Rows;

        var key = P(grid.DataParameterKey);
        PreviewParameters.TryGetValue(key ?? string.Empty, out var json);
        return TableDataBinder.ProjectRows(grid, TableDataBinder.ParseRows(json));
    }

    private static void AppendSanitized(List<string> styles, string key, string? value)
    {
        var css = ReportHtmlSanitizer.SanitizeCss(value);
        if (!string.IsNullOrEmpty(css))
            styles.Add($"{key}: {css}");
    }
}
