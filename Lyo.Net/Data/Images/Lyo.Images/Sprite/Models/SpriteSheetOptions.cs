using System.Diagnostics;

namespace Lyo.Images.Sprite.Models;

/// <summary>Uniform spritesheet layout for calculators and exporters (source size, trims, grid).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SpriteSheetOptions
{
    /// <summary>Source strip or sheet width in pixels.</summary>
    public int SourceWidth { get; init; }

    /// <summary>Source strip or sheet height in pixels.</summary>
    public int SourceHeight { get; init; }

    /// <summary>Horizontal offset into the source where the frame grid starts.</summary>
    public int OffsetX { get; init; }

    /// <summary>Vertical offset into the source where the frame grid starts.</summary>
    public int OffsetY { get; init; }

    /// <summary>Pixels trimmed from the left before measuring cells.</summary>
    public int LeftTrim { get; init; }

    /// <summary>Pixels trimmed from the top before measuring cells.</summary>
    public int TopTrim { get; init; }

    /// <summary>Pixels trimmed from the right edge.</summary>
    public int RightTrim { get; init; }

    /// <summary>Pixels trimmed from the bottom edge.</summary>
    public int BottomTrim { get; init; }

    /// <summary>Rows in the frame grid.</summary>
    public int RowCount { get; init; } = 1;

    /// <summary>Frames placed in each row.</summary>
    public int FramesPerRow { get; init; } = 1;

    /// <summary>Optional cap on how many frames count as present.</summary>
    public int? RequestedFrameCount { get; init; }

    /// <summary>Assumed playback rate used when inferring animation timing.</summary>
    public int FramesPerSecond { get; init; } = 60;

    /// <summary>Zero-based frame indices to skip while walking the grid.</summary>
    public IReadOnlyCollection<int>? ExcludedFrames { get; init; }

    public override string ToString() => $"SpriteSheetOptions: {SourceWidth}x{SourceHeight}, rows={RowCount}, framesPerRow={FramesPerRow}, fps={FramesPerSecond}";
}