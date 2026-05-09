namespace Lyo.Images.Sprite.Models;

/// <summary>Describes a uniform spritesheet layout for calculators and exporters (source dimensions, trims, grid).</summary>
public sealed class SpriteSheetOptions
{
    /// <summary>Total width of the source strip or sheet in pixels.</summary>
    public int SourceWidth { get; init; }

    /// <summary>Total height of the source strip or sheet in pixels.</summary>
    public int SourceHeight { get; init; }

    /// <summary>Horizontal offset into the source where frame grid begins.</summary>
    public int OffsetX { get; init; }

    /// <summary>Vertical offset into the source where frame grid begins.</summary>
    public int OffsetY { get; init; }

    /// <summary>Pixels to trim from the left before measuring cells.</summary>
    public int LeftTrim { get; init; }

    /// <summary>Pixels to trim from the top before measuring cells.</summary>
    public int TopTrim { get; init; }

    /// <summary>Pixels to trim from the right edge.</summary>
    public int RightTrim { get; init; }

    /// <summary>Pixels to trim from the bottom edge.</summary>
    public int BottomTrim { get; init; }

    /// <summary>Number of rows in the frame grid.</summary>
    public int RowCount { get; init; } = 1;

    /// <summary>Number of frames placed in each row.</summary>
    public int FramesPerRow { get; init; } = 1;

    /// <summary>Optional cap on how many frames to treat as present.</summary>
    public int? RequestedFrameCount { get; init; }

    /// <summary>Assumed playback rate when inferring animation timing.</summary>
    public int FramesPerSecond { get; init; } = 60;

    /// <summary>Zero-based frame indices to skip when iterating the grid.</summary>
    public IReadOnlyCollection<int>? ExcludedFrames { get; init; }
}