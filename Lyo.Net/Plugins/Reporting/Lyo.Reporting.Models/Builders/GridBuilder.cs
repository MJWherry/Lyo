using System.Diagnostics;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for a CSS grid of cards, blocks, and tables.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GridBuilder
{
    private readonly Grid _grid = new();

    /// <summary>Sets an optional title above the grid.</summary>
    public GridBuilder SetTitle(string title)
    {
        _grid.Title = title;
        return this;
    }

    /// <summary>Sets equal column count when <see cref="SetTemplateColumns" /> is unset. Default 2.</summary>
    public GridBuilder SetColumnCount(int columnCount)
    {
        _grid.ColumnCount = columnCount;
        return this;
    }

    /// <summary>Sets CSS <c>grid-template-columns</c> (overrides column count when the value is a CSS track list).</summary>
    public GridBuilder SetTemplateColumns(string templateColumns)
    {
        _grid.TemplateColumns = templateColumns;
        return this;
    }

    /// <summary>Sets CSS gap.</summary>
    public GridBuilder SetGap(string gap)
    {
        _grid.Gap = gap;
        return this;
    }

    /// <summary>Adds a CSS style on the grid.</summary>
    public GridBuilder AddStyle(string property, string value)
    {
        _grid.Styles[property] = value;
        return this;
    }

    /// <summary>When true, print/PDF keeps the grid on one page. Default (unset) is on.</summary>
    public GridBuilder SetKeepTogether(bool keepTogether = true)
    {
        _grid.KeepTogether = keepTogether;
        return this;
    }

    /// <summary>Appends a card.</summary>
    public GridBuilder AddCard(string label, object? value, string? width = null, string? alignment = null, bool emphasized = false)
    {
        _grid.TryAdd(
            new Card {
                Label = label,
                Value = value,
                Width = width,
                Alignment = alignment,
                Emphasized = emphasized
            });
        return this;
    }

    /// <summary>Appends a card via a builder.</summary>
    public GridBuilder AddCard(Action<CardBuilder> configure)
    {
        var builder = new CardBuilder();
        configure(builder);
        _grid.TryAdd(builder.Build());
        return this;
    }

    /// <summary>Appends a table.</summary>
    public GridBuilder AddTable(Action<TableBuilder> configure)
    {
        var builder = new TableBuilder();
        configure(builder);
        _grid.TryAdd(builder.Build());
        return this;
    }

    /// <summary>Appends a titled table.</summary>
    public GridBuilder AddTable(string title, Action<TableBuilder>? configure = null)
        => AddTable(tb => {
            tb.SetTitle(title);
            configure?.Invoke(tb);
        });

    /// <summary>Appends a block.</summary>
    public GridBuilder AddBlock(Action<BlockBuilder> configure)
    {
        var builder = new BlockBuilder();
        configure(builder);
        _grid.TryAdd(builder.Build());
        return this;
    }

    /// <summary>Appends a text block.</summary>
    public GridBuilder AddText(string text)
    {
        _grid.TryAdd(new Block { ContentType = ContentType.Text, Content = text });
        return this;
    }

    /// <summary>Builds and returns the finished grid.</summary>
    public Grid Build() => _grid;

    public override string ToString() => $"GridBuilder: {_grid.Title ?? "(Untitled)"} ({_grid.ColumnCount} cols, {_grid.Controls.Count} items)";
}
