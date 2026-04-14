namespace Lyo.Images.Sprite.Models;

public sealed class SpriteSheetOptions
{
    public int SourceWidth { get; init; }

    public int SourceHeight { get; init; }

    public int OffsetX { get; init; }

    public int OffsetY { get; init; }

    public int LeftTrim { get; init; }

    public int TopTrim { get; init; }

    public int RightTrim { get; init; }

    public int BottomTrim { get; init; }

    public int RowCount { get; init; } = 1;

    public int FramesPerRow { get; init; } = 1;

    public int? RequestedFrameCount { get; init; }

    public int FramesPerSecond { get; init; } = 60;

    public IReadOnlyCollection<int>? ExcludedFrames { get; init; }
}