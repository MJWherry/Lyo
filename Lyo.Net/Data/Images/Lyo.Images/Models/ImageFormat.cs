namespace Lyo.Images.Models;

/// <summary>How an image is resized to a target size.</summary>
public enum ResizeMode
{
    /// <summary>Fit inside the size and keep aspect ratio.</summary>
    Max,

    /// <summary>Fill the size and crop overflow.</summary>
    Crop,

    /// <summary>Fill the size and pad leftover space.</summary>
    Pad,

    /// <summary>Fill the size and pad leftover space with a box.</summary>
    BoxPad,

    /// <summary>Stretch to the exact size (aspect ratio may change).</summary>
    Stretch
}