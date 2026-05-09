namespace Lyo.Images.Models;

/// <summary>Options for watermark operations.</summary>
public class WatermarkOptions
{
    /// <summary>Gets or sets the font size in pixels.</summary>
    public int FontSize { get; set; } = 24;

    /// <summary>Gets or sets the font family name.</summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>Gets or sets the text color (hex format, e.g., "#FFFFFF").</summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>Gets or sets the watermark position.</summary>
    public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;

    /// <summary>Gets or sets the opacity (0.0 to 1.0).</summary>
    public float Opacity { get; set; } = 0.7f;

    /// <summary>Gets or sets the padding from edges in pixels.</summary>
    public int Padding { get; set; } = 10;
}

/// <summary>Anchor corner or center for watermark text.</summary>
public enum WatermarkPosition
{
    /// <summary>Upper-left corner (respecting padding).</summary>
    TopLeft,

    /// <summary>Upper-right corner.</summary>
    TopRight,

    /// <summary>Lower-left corner.</summary>
    BottomLeft,

    /// <summary>Lower-right corner.</summary>
    BottomRight,

    /// <summary>Image center.</summary>
    Center
}