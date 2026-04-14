namespace Lyo.Images.Sprite.Models;

/// <summary>Extract timing: <see cref="ImpliedFps" /> is grid cells ÷ one loop in seconds (null for a single-frame source).</summary>
public readonly record struct AnimatedExtractTiming(int SourceFrameCount, double LoopDurationMs, int GridCells, double? ImpliedFps);

/// <summary>How to fill extra grid cells when rows × columns exceeds the sample budget.</summary>
public enum SpriteGridPadMode
{
    /// <summary>Map grid positions evenly onto the sample sequence (duplicates spread through the loop; best for playback).</summary>
    StretchedUniform = 0,

    /// <summary>Split padding: repeat the first sample before the sequence and the last sample after it (equal counts when possible).</summary>
    SymmetricBookends = 1,

    /// <summary>Repeat only the last sample at the end of the grid.</summary>
    EndRepeatLast = 2
}