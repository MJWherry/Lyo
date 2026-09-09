using System.Diagnostics;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for KPI cards.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class CardBuilder
{
    private readonly Card _card = new();

    /// <summary>Sets the card's label.</summary>
    public CardBuilder SetLabel(string label)
    {
        _card.Label = label;
        return this;
    }

    /// <summary>Sets the card's value.</summary>
    public CardBuilder SetValue(object? value)
    {
        _card.Value = value;
        return this;
    }

    /// <summary>Sets the card width as a CSS value (for example "50%", "200px").</summary>
    public CardBuilder SetWidth(string width)
    {
        _card.Width = width;
        return this;
    }

    /// <summary>Sets alignment (left, right, center, or justify).</summary>
    public CardBuilder SetAlignment(string alignment)
    {
        _card.Alignment = alignment;
        return this;
    }

    /// <summary>Sets whether the value is emphasized.</summary>
    public CardBuilder SetEmphasized(bool emphasized = true)
    {
        _card.Emphasized = emphasized;
        return this;
    }

    /// <summary>Adds a CSS style on the card.</summary>
    public CardBuilder AddStyle(string property, string value)
    {
        _card.Styles[property] = value;
        return this;
    }

    /// <summary>Sets a formatter for the value.</summary>
    public CardBuilder SetFormatter(Func<object?, string> formatter)
    {
        _card.ValueFormatter = formatter;
        return this;
    }

    /// <summary>How many grid columns this card spans when it is a grid child.</summary>
    public CardBuilder SetColumnSpan(int span)
    {
        _card.ColumnSpan = span;
        return this;
    }

    /// <summary>Builds and returns the finished card.</summary>
    public Card Build() => _card;

    public override string ToString() => $"CardBuilder: {_card.Label ?? "(No Label)"} = {_card.Value}";
}
