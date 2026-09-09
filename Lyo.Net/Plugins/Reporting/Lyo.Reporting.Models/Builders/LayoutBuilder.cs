using System.Diagnostics;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for <see cref="Layout" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LayoutBuilder
{
    private readonly Layout _layout = new();

    /// <summary>Sets the accent color as a CSS value.</summary>
    public LayoutBuilder SetAccentColor(string color)
    {
        _layout.AccentColor = color;
        return this;
    }

    /// <summary>Sets the page size hint (<c>Letter</c>, <c>A4</c>, or <c>Auto</c>).</summary>
    public LayoutBuilder SetPageSize(string pageSize)
    {
        _layout.PageSize = pageSize;
        return this;
    }

    /// <summary>Sets the orientation hint (<c>Portrait</c> or <c>Landscape</c>).</summary>
    public LayoutBuilder SetOrientation(string orientation)
    {
        _layout.Orientation = orientation;
        return this;
    }

    /// <summary>Sets the visual theme (<c>Default</c>, <c>Compact</c>, or <c>Formal</c>).</summary>
    public LayoutBuilder SetTheme(string theme)
    {
        _layout.Theme = theme;
        return this;
    }

    /// <summary>Sets header band text.</summary>
    public LayoutBuilder SetHeaderText(string headerText)
    {
        _layout.HeaderText = headerText;
        return this;
    }

    /// <summary>Sets the header logo URL.</summary>
    public LayoutBuilder SetLogoUrl(string logoUrl)
    {
        _layout.LogoUrl = logoUrl;
        return this;
    }

    /// <summary>Sets watermark text.</summary>
    public LayoutBuilder SetWatermark(string watermark)
    {
        _layout.Watermark = watermark;
        return this;
    }

    /// <summary>Shows a page-number placeholder in the footer.</summary>
    public LayoutBuilder ShowPageNumbers(bool show = true)
    {
        _layout.ShowPageNumbers = show;
        return this;
    }

    /// <summary>Sets body padding shorthand (for example <c>24px</c>).</summary>
    public LayoutBuilder SetPadding(string padding)
    {
        _layout.Padding = padding;
        return this;
    }

    /// <summary>Sets print/PDF <c>@page</c> margin shorthand (for example <c>1in</c>).</summary>
    public LayoutBuilder SetMargin(string margin)
    {
        _layout.Margin = margin;
        return this;
    }

    /// <summary>Sets an optional full-page root component FullName.</summary>
    public LayoutBuilder SetRootComponentType(string typeFullName)
    {
        _layout.RootComponentType = typeFullName;
        return this;
    }

    /// <summary>Builds the layout.</summary>
    public Layout Build() => _layout;

    public override string ToString() => _layout.ToString();
}
