using Lyo.Images.Sprite.Models;

namespace Lyo.Images.Sprite;

public sealed class SpriteSheetCalculation
{
    public required IReadOnlyList<SpriteFrameRect> Frames { get; init; }

    public required IReadOnlyList<SpriteFrameRect> IncludedFrames { get; init; }

    public int SourceWidth { get; init; }

    public int SourceHeight { get; init; }

    public int FrameWidth { get; init; }

    public int FrameHeight { get; init; }

    public int RowCount { get; init; }

    public int ColumnCount { get; init; }

    public int FramesPerSecond { get; init; }

    public int OffsetX { get; init; }

    public int OffsetY { get; init; }

    public int LeftTrim { get; init; }

    public int TopTrim { get; init; }

    public int RightTrim { get; init; }

    public int BottomTrim { get; init; }

    public int MaxFrameCount { get; init; }

    public bool HasFrames => Frames.Count > 0;
}