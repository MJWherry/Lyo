using System.Diagnostics;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Builders;

/// <summary>Fluent builder for constructing report sections.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SectionBuilder
{
    private readonly ReportSection _section = new();

    /// <summary>Sets the section title.</summary>
    public SectionBuilder SetTitle(string title)
    {
        _section.Title = title;
        return this;
    }

    /// <summary>Sets the section subtitle.</summary>
    public SectionBuilder SetSubtitle(string subtitle)
    {
        _section.Subtitle = subtitle;
        return this;
    }

    /// <summary>Sets the section description.</summary>
    public SectionBuilder SetDescription(string description)
    {
        _section.Description = description;
        return this;
    }

    /// <summary>Sets whether the section should be collapsed by default.</summary>
    public SectionBuilder SetCollapsed(bool collapsed = true)
    {
        _section.Collapsed = collapsed;
        return this;
    }

    /// <summary>Adds a CSS style to the section.</summary>
    public SectionBuilder AddStyle(string property, string value)
    {
        _section.Styles[property] = value;
        return this;
    }

    /// <summary>Adds a column to the section.</summary>
    public SectionBuilder AddColumn(string label, object? value, string? width = null, string? alignment = null)
    {
        _section.Columns.Add(
            new() {
                Label = label,
                Value = value,
                Width = width,
                Alignment = alignment
            });

        return this;
    }

    /// <summary>Adds a column using a column builder.</summary>
    public SectionBuilder AddColumn(Action<ColumnBuilder> configure)
    {
        var builder = new ColumnBuilder();
        configure(builder);
        _section.Columns.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a column using a column builder instance.</summary>
    public SectionBuilder AddColumn(ColumnBuilder columnBuilder)
    {
        _section.Columns.Add(columnBuilder.Build());
        return this;
    }

    /// <summary>Adds multiple columns from a dictionary (label-value pairs).</summary>
    public SectionBuilder AddColumns(Dictionary<string, object?> columns)
    {
        foreach (var kvp in columns)
            AddColumn(kvp.Key, kvp.Value);

        return this;
    }

    /// <summary>Adds a grid/table to the section using a configuration action.</summary>
    public SectionBuilder AddGrid(Action<GridBuilder> configure)
    {
        var builder = new GridBuilder();
        configure(builder);
        _section.Grids.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a grid/table to the section using a grid builder.</summary>
    public SectionBuilder AddGrid(GridBuilder gridBuilder)
    {
        _section.Grids.Add(gridBuilder.Build());
        return this;
    }

    /// <summary>Adds a grid with a title.</summary>
    public SectionBuilder AddGrid(string title, Action<GridBuilder>? configure = null)
        => AddGrid(gb => {
            gb.SetTitle(title);
            configure?.Invoke(gb);
        });

    /// <summary>Adds a nested subsection.</summary>
    public SectionBuilder AddSubsection(Action<SectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        var subsection = builder.Build();
        subsection.Order = _section.Subsections.Count + 1;
        _section.Subsections.Add(subsection);
        return this;
    }

    /// <summary>Adds a nested subsection using a section builder.</summary>
    public SectionBuilder AddSubsection(SectionBuilder sectionBuilder)
    {
        var subsection = sectionBuilder.Build();
        subsection.Order = _section.Subsections.Count + 1;
        _section.Subsections.Add(subsection);
        return this;
    }

    /// <summary>Adds a nested subsection with a title.</summary>
    public SectionBuilder AddSubsection(string title, Action<SectionBuilder>? configure = null)
        => AddSubsection(sb => {
            sb.SetTitle(title);
            configure?.Invoke(sb);
        });

    /// <summary>Adds a content block to the section.</summary>
    public SectionBuilder AddContentBlock(ReportContentType contentType, string content)
    {
        _section.ContentBlocks.Add(new() { ContentType = contentType, Content = content });
        return this;
    }

    /// <summary>Adds a content block using a content block builder.</summary>
    public SectionBuilder AddContentBlock(Action<ContentBlockBuilder> configure)
    {
        var builder = new ContentBlockBuilder();
        configure(builder);
        _section.ContentBlocks.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a text content block.</summary>
    public SectionBuilder AddText(string text) => AddContentBlock(ReportContentType.Text, text);

    /// <summary>Adds an HTML content block.</summary>
    public SectionBuilder AddHtml(string html) => AddContentBlock(ReportContentType.Html, html);

    /// <summary>Adds a list content block.</summary>
    public SectionBuilder AddList(params string[] items) => AddList((IEnumerable<string>)items);

    /// <summary>Adds a list content block.</summary>
    public SectionBuilder AddList(IEnumerable<string> items)
    {
        _section.ContentBlocks.Add(new() { ContentType = ReportContentType.List, ListItems = items.ToList() });
        return this;
    }

    /// <summary>Adds a numbered list content block.</summary>
    public SectionBuilder AddNumberedList(params string[] items) => AddNumberedList((IEnumerable<string>)items);

    /// <summary>Adds a numbered list content block.</summary>
    public SectionBuilder AddNumberedList(IEnumerable<string> items)
    {
        _section.ContentBlocks.Add(new() { ContentType = ReportContentType.NumberedList, ListItems = items.ToList() });
        return this;
    }

    /// <summary>Builds and returns the section.</summary>
    public ReportSection Build() => _section;

    public override string ToString() => $"SectionBuilder: {_section.Title ?? "(Untitled)"} ({_section.Columns.Count} columns, {_section.Grids.Count} grids)";
}