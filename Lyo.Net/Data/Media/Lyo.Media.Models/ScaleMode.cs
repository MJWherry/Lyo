namespace Lyo.Media.Models;

/// <summary>How <see cref="VideoConversionOptions.Width" /> and <see cref="VideoConversionOptions.Height" /> are applied.</summary>
public enum ScaleMode
{
    /// <summary>Stretch to the exact size with <c>-s</c>.</summary>
    Stretch = 0,

    /// <summary>Fit inside the box and keep the aspect ratio.</summary>
    Fit = 1,

    /// <summary>Fill the box and crop the overflow.</summary>
    Fill = 2
}
