using System.Diagnostics;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for report sections.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SectionBuilder
{
    private readonly Section _section = new();

    /// <summary>Sets the section's title.</summary>
    public SectionBuilder SetTitle(string title)
    {
        _section.Title = title;
        return this;
    }

    /// <summary>Sets the section's subtitle.</summary>
    public SectionBuilder SetSubtitle(string subtitle)
    {
        _section.Subtitle = subtitle;
        return this;
    }

    /// <summary>Sets the section's description.</summary>
    public SectionBuilder SetDescription(string description)
    {
        _section.Description = description;
        return this;
    }

    /// <summary>Sets whether the section starts collapsed.</summary>
    public SectionBuilder SetCollapsed(bool collapsed = true)
    {
        _section.Collapsed = collapsed;
        return this;
    }

    /// <summary>Sets a stable section id for the designer.</summary>
    public SectionBuilder SetId(string id)
    {
        _section.Id = id;
        return this;
    }

    /// <summary>Sets a visibility condition evaluated against bound parameters.</summary>
    public SectionBuilder SetVisibleWhen(string visibleWhen)
    {
        _section.VisibleWhen = visibleWhen;
        return this;
    }

    /// <summary>Adds a CSS style on the section.</summary>
    public SectionBuilder AddStyle(string property, string value)
    {
        _section.Styles[property] = value;
        return this;
    }

    /// <summary>Appends a card to the section.</summary>
    public SectionBuilder AddCard(string label, object? value, string? width = null, string? alignment = null, bool emphasized = false)
    {
        _section.Controls.Add(
            new Card {
                Label = label,
                Value = value,
                Width = width,
                Alignment = alignment,
                Emphasized = emphasized
            });
        return this;
    }

    /// <summary>Appends a card via a card builder.</summary>
    public SectionBuilder AddCard(Action<CardBuilder> configure)
    {
        var builder = new CardBuilder();
        configure(builder);
        _section.Controls.Add(builder.Build());
        return this;
    }

    /// <summary>Appends a card from a card builder instance.</summary>
    public SectionBuilder AddCard(CardBuilder cardBuilder)
    {
        _section.Controls.Add(cardBuilder.Build());
        return this;
    }

    /// <summary>Adds a CSS grid via a configuration action.</summary>
    public SectionBuilder AddGrid(Action<GridBuilder> configure)
    {
        var builder = new GridBuilder();
        configure(builder);
        _section.Controls.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a CSS grid from a grid builder.</summary>
    public SectionBuilder AddGrid(GridBuilder gridBuilder)
    {
        _section.Controls.Add(gridBuilder.Build());
        return this;
    }

    /// <summary>Adds a titled CSS grid.</summary>
    public SectionBuilder AddGrid(string title, Action<GridBuilder>? configure = null)
        => AddGrid(gb => {
            gb.SetTitle(title);
            configure?.Invoke(gb);
        });

    /// <summary>Adds a table via a configuration action.</summary>
    public SectionBuilder AddTable(Action<TableBuilder> configure)
    {
        var builder = new TableBuilder();
        configure(builder);
        _section.Controls.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a table from a table builder.</summary>
    public SectionBuilder AddTable(TableBuilder tableBuilder)
    {
        _section.Controls.Add(tableBuilder.Build());
        return this;
    }

    /// <summary>Adds a titled table.</summary>
    public SectionBuilder AddTable(string title, Action<TableBuilder>? configure = null)
        => AddTable(tb => {
            tb.SetTitle(title);
            configure?.Invoke(tb);
        });

    /// <summary>Adds a nested child subsection.</summary>
    public SectionBuilder AddSubsection(Action<SectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        var subsection = builder.Build();
        subsection.Order = _section.Subsections.Count + 1;
        _section.Subsections.Add(subsection);
        return this;
    }

    /// <summary>Adds a nested subsection from a section builder.</summary>
    public SectionBuilder AddSubsection(SectionBuilder sectionBuilder)
    {
        var subsection = sectionBuilder.Build();
        subsection.Order = _section.Subsections.Count + 1;
        _section.Subsections.Add(subsection);
        return this;
    }

    /// <summary>Adds a titled nested subsection.</summary>
    public SectionBuilder AddSubsection(string title, Action<SectionBuilder>? configure = null)
        => AddSubsection(sb => {
            sb.SetTitle(title);
            configure?.Invoke(sb);
        });

    /// <summary>Appends a block to the section.</summary>
    public SectionBuilder AddBlock(ContentType contentType, string content)
    {
        _section.Controls.Add(new Block { ContentType = contentType, Content = content });
        return this;
    }

    /// <summary>Appends a block from a block builder.</summary>
    public SectionBuilder AddBlock(Action<BlockBuilder> configure)
    {
        var builder = new BlockBuilder();
        configure(builder);
        _section.Controls.Add(builder.Build());
        return this;
    }

    /// <summary>Appends a text block.</summary>
    public SectionBuilder AddText(string text) => AddBlock(ContentType.Text, text);

    /// <summary>Appends an HTML block.</summary>
    public SectionBuilder AddHtml(string html) => AddBlock(ContentType.Html, html);

    /// <summary>Appends a list block.</summary>
    public SectionBuilder AddList(params string[] items) => AddList((IEnumerable<string>)items);

    /// <summary>Appends a list block.</summary>
    public SectionBuilder AddList(IEnumerable<string> items)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.List, ListItems = items.ToList() });
        return this;
    }

    /// <summary>Appends a numbered list block.</summary>
    public SectionBuilder AddNumberedList(params string[] items) => AddNumberedList((IEnumerable<string>)items);

    /// <summary>Appends a numbered list block.</summary>
    public SectionBuilder AddNumberedList(IEnumerable<string> items)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.NumberedList, ListItems = items.ToList() });
        return this;
    }

    /// <summary>Appends a quote block.</summary>
    public SectionBuilder AddQuote(string quote) => AddBlock(ContentType.Quote, quote);

    /// <summary>Appends a code block.</summary>
    public SectionBuilder AddCode(string code) => AddBlock(ContentType.Code, code);

    /// <summary>Appends an image block.</summary>
    public SectionBuilder AddImage(string source, string? alt = null, string? caption = null)
    {
        _section.Controls.Add(
            new Block {
                ContentType = ContentType.Image,
                Source = source,
                Alt = alt,
                Caption = caption
            });
        return this;
    }

    /// <summary>Appends a horizontal divider.</summary>
    public SectionBuilder AddDivider()
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Divider });
        return this;
    }

    /// <summary>Appends a callout (info, success, warning, or error).</summary>
    public SectionBuilder AddCallout(string text, string tone = "info")
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Callout, Content = text, Tone = tone });
        return this;
    }

    /// <summary>Appends a page-break hint for PDF output.</summary>
    public SectionBuilder AddPageBreak()
    {
        _section.Controls.Add(new Block { ContentType = ContentType.PageBreak });
        return this;
    }

    /// <summary>Appends vertical space. <paramref name="pixels" /> is stored as <see cref="Block.Level" />.</summary>
    public SectionBuilder AddSpacer(int pixels = 24)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Spacer, Level = pixels });
        return this;
    }

    /// <summary>Appends a progress bar. <paramref name="percent" /> is 0–100.</summary>
    public SectionBuilder AddProgress(int percent, string? caption = null)
    {
        _section.Controls.Add(
            new Block {
                ContentType = ContentType.Progress,
                Level = percent < 0 ? 0 : percent > 100 ? 100 : percent,
                Caption = caption
            });
        return this;
    }

    /// <summary>Appends a heading. <paramref name="level" /> is 1–6.</summary>
    public SectionBuilder AddHeading(string text, int level = 3)
    {
        _section.Controls.Add(
            new Block {
                ContentType = ContentType.Heading,
                Content = text,
                Level = level < 1 ? 1 : level > 6 ? 6 : level
            });
        return this;
    }

    /// <summary>Appends a custom component block resolved by CLR FullName.</summary>
    public SectionBuilder AddComponent(string componentType, Dictionary<string, ComponentBinding>? bindings = null)
    {
        _section.Controls.Add(
            new Block {
                ContentType = ContentType.Component,
                ComponentType = componentType,
                ParameterBindings = bindings ?? []
            });
        return this;
    }

    /// <summary>Appends a key-value row. Additional calls append to the last key-value block, or start a new one.</summary>
    public SectionBuilder AddKeyValue(string label, string? value)
    {
        var item = $"{label}|{value}";
        var last = _section.Controls.Count > 0 ? _section.Controls[^1] as Block : null;
        if (last is { ContentType: ContentType.KeyValue }) {
            last.ListItems ??= [];
            last.ListItems.Add(item);
            return this;
        }

        _section.Controls.Add(new Block { ContentType = ContentType.KeyValue, ListItems = [item] });
        return this;
    }

    /// <summary>Appends a structured Chart.js block. Series are <c>label|value</c> rows in <see cref="Block.ListItems" />.</summary>
    public SectionBuilder AddChart(ChartKind kind, string? title = null, IEnumerable<string>? series = null, int height = 300)
    {
        _section.Controls.Add(
            new Block {
                ContentType = ContentType.Chart,
                ChartKind = kind,
                Caption = title,
                Level = height,
                ListItems = series?.ToList() ?? []
            });
        return this;
    }

    /// <summary>
    /// Chart series from a report parameter whose value is a JSON array of objects.
    /// The live canvas fills from example JSON without mutating the block.
    /// </summary>
    public SectionBuilder AddChartFromParameter(
        ChartKind kind,
        string parameterKey,
        string? title = null,
        string labelField = "label",
        string valueField = "value",
        int height = 300)
    {
        _section.Controls.Add(
            new Block {
                ContentType = ContentType.Chart,
                ChartKind = kind,
                Caption = title,
                Level = height,
                DataSourceKind = DataSourceKind.FromParameter,
                DataParameterKey = parameterKey,
                ChartLabelField = labelField,
                ChartValueField = valueField
            });
        return this;
    }

    /// <summary>Appends a compact status pill.</summary>
    public SectionBuilder AddBadge(string text, string tone = "info")
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Badge, Content = text, Tone = tone });
        return this;
    }

    /// <summary>Appends a print-friendly signature line. <paramref name="name" /> is the signed name; <paramref name="dateLabel" /> is the date line.</summary>
    public SectionBuilder AddSignature(string? caption = "Authorized by", string? name = null, string? dateLabel = "Date")
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Signature, Caption = caption, Content = name, Source = dateLabel });
        return this;
    }

    /// <summary>Appends a table of contents. Empty <paramref name="items" /> means auto-fill from section titles and headings at bind time.</summary>
    public SectionBuilder AddTableOfContents(string? caption = "Contents", IEnumerable<string>? items = null)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.TableOfContents, Caption = caption, ListItems = items?.ToList() });
        return this;
    }

    /// <summary>Appends a vertical timeline. Steps are <c>label|detail</c>.</summary>
    public SectionBuilder AddTimeline(IEnumerable<string> steps)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Timeline, ListItems = steps.ToList() });
        return this;
    }

    /// <summary>Appends a stacked address block. <paramref name="caption" /> is typically Bill to / Ship to; <paramref name="lines" /> are name, street, city.</summary>
    public SectionBuilder AddAddress(string caption, IEnumerable<string> lines)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Address, Caption = caption, ListItems = lines.ToList() });
        return this;
    }

    /// <summary>Appends a totals stack. Rows are <c>label|amount</c>; the last row is emphasized.</summary>
    public SectionBuilder AddTotals(IEnumerable<string> rows)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Totals, ListItems = rows.ToList() });
        return this;
    }

    /// <summary>Appends print-friendly empty checkboxes with term lines. <paramref name="intro" /> is optional lead-in text.</summary>
    public SectionBuilder AddCheckbox(IEnumerable<string> terms, string? intro = null)
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Checkbox, Content = intro, ListItems = terms.ToList() });
        return this;
    }

    /// <summary>Appends a muted notes box. Not a callout (no tone) and not a quote.</summary>
    public SectionBuilder AddNotes(string body, string? caption = "Notes")
    {
        _section.Controls.Add(new Block { ContentType = ContentType.Notes, Caption = caption, Content = body });
        return this;
    }

    /// <summary>Builds and returns the finished section.</summary>
    public Section Build() => _section;

    public override string ToString() => $"SectionBuilder: {_section.Title ?? "(Untitled)"} ({_section.Controls.Count} controls)";
}
