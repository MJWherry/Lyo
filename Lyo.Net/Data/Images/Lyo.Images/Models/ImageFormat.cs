namespace Lyo.Images.Models;

/// <summary>Resize mode for image resizing operations.</summary>
public enum ResizeMode
{
    /// <summary>Resize to fit within dimensions while maintaining aspect ratio.</summary>
    Max,

    /// <summary>Resize to fill dimensions, cropping if necessary.</summary>
    Crop,

    /// <summary>Resize to fill dimensions, padding if necessary.</summary>
    Pad,

    /// <summary>Resize to fill dimensions, padding with box if necessary.</summary>
    BoxPad,

    /// <summary>Stretch to exact dimensions (may distort aspect ratio).</summary>
    Stretch
}