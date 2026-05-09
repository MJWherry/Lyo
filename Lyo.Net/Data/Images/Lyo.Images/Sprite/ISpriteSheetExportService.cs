using Lyo.Images.Models;
using Lyo.Images.Sprite.Models;

namespace Lyo.Images.Sprite;

/// <summary>Exports spritesheets, animated GIFs, and frame crops from flat strips and animated sources.</summary>
public interface ISpriteSheetExportService
{
    /// <summary>Reads dimensions/format metadata from a spritesheet or animation byte payload.</summary>
    /// <param name="imageBytes">Encoded image bytes (PNG, GIF, WebP, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ImageMetadata> GetMetadataAsync(byte[] imageBytes, CancellationToken ct = default);

    /// <summary>Crops a single frame region to a new raster (typically PNG bytes).</summary>
    /// <param name="imageBytes">Source image bytes.</param>
    /// <param name="frame">Pixel rectangle and optional per-frame duration metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> ExportFrameAsync(byte[] imageBytes, SpriteFrameRect frame, CancellationToken ct = default);

    /// <summary>Exports multiple frame crops as a ZIP archive of image files.</summary>
    /// <param name="imageBytes">Source image bytes.</param>
    /// <param name="frames">Frame rectangles to extract.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> ExportFramesZipAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, CancellationToken ct = default);

    /// <summary>Builds an animated GIF from ordered frame rectangles.</summary>
    /// <param name="imageBytes">Source image bytes.</param>
    /// <param name="frames">Frames in playback order.</param>
    /// <param name="framesPerSecond">Playback rate for the output GIF.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> ExportAnimatedGifAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, int framesPerSecond, CancellationToken ct = default);

    /// <summary>Lays out sampled frames from an animated source into a single PNG spritesheet grid.</summary>
    /// <param name="imageBytes">Animated source bytes.</param>
    /// <param name="sampleBudget">Maximum number of frames to sample from the source.</param>
    /// <param name="rowCount">Number of rows in the output grid.</param>
    /// <param name="framesPerRow">Frames placed per row.</param>
    /// <param name="offsetX">Pixel offset from left edge of the logical sheet.</param>
    /// <param name="offsetY">Pixel offset from top edge of the logical sheet.</param>
    /// <param name="padLeft">Left padding between cells.</param>
    /// <param name="padRight">Right padding between cells.</param>
    /// <param name="padTop">Top padding between cells.</param>
    /// <param name="padBottom">Bottom padding between cells.</param>
    /// <param name="gridPadMode">How padding is distributed between cells.</param>
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

    /// <summary>Returns how many frames the animated source contains and its loop duration in milliseconds.</summary>
    /// <param name="imageBytes">Animated source bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<(int SourceFrameCount, double LoopDurationMs)> GetAnimatedSourceStatsAsync(byte[] imageBytes, CancellationToken ct = default);

    /// <summary>Estimates timing for extracting a spritesheet grid from an animated source.</summary>
    /// <param name="imageBytes">Animated source bytes.</param>
    /// <param name="rowCount">Target row count for the grid.</param>
    /// <param name="framesPerRow">Frames per row in the grid.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AnimatedExtractTiming> GetAnimatedExtractTimingAsync(byte[] imageBytes, int rowCount, int framesPerRow, CancellationToken ct = default);
}