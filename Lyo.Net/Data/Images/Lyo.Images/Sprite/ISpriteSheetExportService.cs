using Lyo.Images.Models;
using Lyo.Images.Sprite.Models;

namespace Lyo.Images.Sprite;

/// <summary>Exports spritesheets, animated GIFs, and frame crops from flat strips and animated sources.</summary>
public interface ISpriteSheetExportService
{
    Task<ImageMetadata> GetMetadataAsync(byte[] imageBytes, CancellationToken ct = default);

    Task<byte[]> ExportFrameAsync(byte[] imageBytes, SpriteFrameRect frame, CancellationToken ct = default);

    Task<byte[]> ExportFramesZipAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, CancellationToken ct = default);

    Task<byte[]> ExportAnimatedGifAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, int framesPerSecond, CancellationToken ct = default);

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

    Task<(int SourceFrameCount, double LoopDurationMs)> GetAnimatedSourceStatsAsync(byte[] imageBytes, CancellationToken ct = default);

    Task<AnimatedExtractTiming> GetAnimatedExtractTimingAsync(byte[] imageBytes, int rowCount, int framesPerRow, CancellationToken ct = default);
}