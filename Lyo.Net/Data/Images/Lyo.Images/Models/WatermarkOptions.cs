using System.Diagnostics;

namespace Lyo.Images.Models;

/// <summary>Settings for watermark operations.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class WatermarkOptions
{
    /// <summary>Font size in pixels.</summary>
    public int FontSize { get; set; } = 24;

    /// <summary>Font family name.</summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>Text color (hex, e.g., "#FFFFFF").</summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>Watermark position.</summary>
    public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;

    /// <summary>Opacity from 0.0 to 1.0.</summary>
    public float Opacity { get; set; } = 0.7f;

    /// <summary>Padding from edges in pixels.</summary>
    public int Padding { get; set; } = 10;

    public override string ToString() => $"WatermarkOptions: position={Position}, fontSize={FontSize}, opacity={Opacity}";
}

/// <summary>Corner or center anchor for watermark text.</summary>
public enum WatermarkPosition
{
    /// <summary>Upper-left corner (honors padding).</summary>
    TopLeft,

    /// <summary>Upper-right corner.</summary>
    TopRight,

    /// <summary>Lower-left corner.</summary>
    BottomLeft,

    /// <summary>Lower-right corner.</summary>
    BottomRight,

    /// <summary>Center of the image.</summary>
    Center
}