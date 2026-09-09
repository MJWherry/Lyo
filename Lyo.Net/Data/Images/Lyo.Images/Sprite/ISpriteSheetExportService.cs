using Lyo.Images.Models;
using Lyo.Images.Sprite.Models;

namespace Lyo.Images.Sprite;

/// <summary>Exports spritesheets, animated GIFs, and frame crops from strips and animated sources.</summary>
public interface ISpriteSheetExportService
{
    /// <summary>Reads size and format metadata from spritesheet or animation bytes.</summary>
    /// <param name="imageBytes">Encoded image bytes (PNG, GIF, WebP, and similar).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ImageMetadata> GetMetadataAsync(byte[] imageBytes, CancellationToken ct = default);

    /// <summary>Crops one frame region to a new raster (usually PNG bytes).</summary>
    /// <param name="imageBytes">Source image bytes.</param>
    /// <param name="frame">Pixel rectangle and optional per-frame duration.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> ExportFrameAsync(byte[] imageBytes, SpriteFrameRect frame, CancellationToken ct = default);

    /// <summary>Exports several frame crops as a ZIP of image files.</summary>
    /// <param name="imageBytes">Source image bytes.</param>
    /// <param name="frames">Frame rectangles to extract.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> ExportFramesZipAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, CancellationToken ct = default);

    /// <summary>Builds an animated GIF from frame rectangles in playback order.</summary>
    /// <param name="imageBytes">Source image bytes.</param>
    /// <param name="frames">Frames in playback order.</param>
    /// <param name="framesPerSecond">Playback rate for the output GIF.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> ExportAnimatedGifAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, int framesPerSecond, CancellationToken ct = default);

    /// <summary>Lays sampled frames from an animated source into one PNG spritesheet grid.</summary>
    /// <param name="imageBytes">Animated source bytes.</param>
    /// <param name="sampleBudget">Max frames to sample from the source.</param>
    /// <param name="rowCount">Rows in the output grid.</param>
    /// <param name="framesPerRow">Frames placed per row.</param>
    /// <param name="offsetX">Pixel offset from the left of the logical sheet.</param>
    /// <param name="offsetY">Pixel offset from the top of the logical sheet.</param>
    /// <param name="padLeft">Left padding between cells.</param>
    /// <param name="padRight">Right padding between cells.</param>
    /// <param name="padTop">Top padding between cells.</param>
    /// <param name="padBottom">Bottom padding between cells.</param>
    /// <param name="gridPadMode">How padding is spread between cells.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> ExportAnimatedImageToSpriteSheetPngAsync(
        byte[] imageBytes,
        int sampleBudget,
        int rowCount,
        int framesPerRow,
        int offsetX,
        int offsetY,
        int padLeft,
        int padRight,
        int padTop,
        int padBottom,
        SpriteGridPadMode gridPadMode = SpriteGridPadMode.StretchedUniform,
        CancellationToken ct = default);

    /// <summary>Frame count of the animated source and its loop duration in milliseconds.</summary>
    /// <param name="imageBytes">Animated source bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<(int SourceFrameCount, double LoopDurationMs)> GetAnimatedSourceStatsAsync(byte[] imageBytes, CancellationToken ct = default);

    /// <summary>Estimates timing for pulling a spritesheet grid from an animated source.</summary>
    /// <param name="imageBytes">Animated source bytes.</param>
    /// <param name="rowCount">Target row count for the grid.</param>
    /// <param name="framesPerRow">Frames per row in the grid.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AnimatedExtractTiming> GetAnimatedExtractTimingAsync(byte[] imageBytes, int rowCount, int framesPerRow, CancellationToken ct = default);
}