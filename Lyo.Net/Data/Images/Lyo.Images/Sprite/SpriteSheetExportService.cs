using System.IO.Compression;
using System.Text.Json;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Images.Models;
using Lyo.Images.Sprite.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lyo.Images.Sprite;

public sealed class SpriteSheetExportService(IImageService imageService) : ISpriteSheetExportService
{
    public const string PngGridTextKeyword = "LyoGrid";

    /// <summary>Default FPS for flat spritesheets or when animation timing is unavailable.</summary>
    public const int DefaultSpriteSheetPlaybackFps = 60;

    public async Task<ImageMetadata> GetMetadataAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        await using var sourceStream = new MemoryStream(imageBytes, false);
        var result = await imageService.GetMetadataAsync(sourceStream, ct);
        if (!result.IsSuccess || result.Data == null)
            throw new InvalidOperationException(FormatErrors(result.Errors));

        return result.Data;
    }

    public async Task<byte[]> ExportFrameAsync(byte[] imageBytes, SpriteFrameRect frame, CancellationToken ct = default)
    {
        await using var sourceStream = new MemoryStream(imageBytes, false);
        await using var outputStream = new MemoryStream();
        var result = await imageService.CropAsync(sourceStream, outputStream, frame.X, frame.Y, frame.Width, frame.Height, ImageFormat.Png, ct: ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException(FormatErrors(result.Errors));

        return outputStream.ToArray();
    }

    public async Task<byte[]> ExportFramesZipAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, CancellationToken ct = default)
    {
        await using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true)) {
            foreach (var frame in frames.Where(frame => frame.IsIncluded)) {
                ct.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry($"frame-{frame.Index:D3}.png", CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                var frameBytes = await ExportFrameAsync(imageBytes, frame, ct);
                await entryStream.WriteAsync(frameBytes, ct);
            }
        }

        return zipStream.ToArray();
    }

    public async Task<byte[]> ExportAnimatedGifAsync(byte[] imageBytes, IReadOnlyList<SpriteFrameRect> frames, int framesPerSecond, CancellationToken ct = default)
    {
        var includedFrames = frames.Where(frame => frame.IsIncluded).ToList();
        if (includedFrames.Count == 0)
            throw new InvalidOperationException("Add at least one included frame before exporting a GIF.");

        var frameDelay = Math.Max(1, (int)Math.Round(100d / Math.Clamp(framesPerSecond, 1, 60)));
        await using var sourceStream = new MemoryStream(imageBytes, false);
        using var spriteSheet = await Image.LoadAsync<Rgba32>(sourceStream, ct);
        using var gif = spriteSheet.Clone(context => context.Crop(new(includedFrames[0].X, includedFrames[0].Y, includedFrames[0].Width, includedFrames[0].Height)));
        gif.Metadata.GetGifMetadata().RepeatCount = 0;
        gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelay;
        foreach (var frame in includedFrames.Skip(1)) {
            using var frameImage = spriteSheet.Clone(context => context.Crop(new(frame.X, frame.Y, frame.Width, frame.Height)));
            frameImage.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelay;
            gif.Frames.AddFrame(frameImage.Frames.RootFrame);
        }

        await using var outputStream = new MemoryStream();
        await gif.SaveAsGifAsync(outputStream, new(), ct);
        return outputStream.ToArray();
    }

    /// <inheritdoc />
    public Task<byte[]> ExportAnimatedImageToSpriteSheetPngAsync(
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
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        return ExportAnimatedImageToSpriteSheetPngCoreAsync(
            imageBytes, sampleBudget, rowCount, framesPerRow, offsetX, offsetY, padLeft, padRight, padTop, padBottom, gridPadMode, ct);
    }

    public async Task<(int SourceFrameCount, double LoopDurationMs)> GetAnimatedSourceStatsAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        await using var stream = new MemoryStream(imageBytes, false);
        using var image = await Image.LoadAsync<Rgba32>(stream, ct);
        if (image.Frames.Count == 0)
            return (0, 0);

        var fc = image.Frames.Count;
        if (fc == 1)
            return (1, 0);

        double totalMs = 0;
        for (var i = 0; i < image.Frames.Count; i++)
            totalMs += GetFrameDelayMs(image.Frames[i]);

        return (fc, totalMs <= 0 ? 0 : totalMs);
    }

    public async Task<AnimatedExtractTiming> GetAnimatedExtractTimingAsync(byte[] imageBytes, int rowCount, int framesPerRow, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        rowCount = Math.Clamp(rowCount, 1, 256);
        framesPerRow = Math.Clamp(framesPerRow, 1, 4096);
        var gridCells = rowCount * framesPerRow;
        await using var stream = new MemoryStream(imageBytes, false);
        using var image = await Image.LoadAsync<Rgba32>(stream, ct);
        if (image.Frames.Count == 0)
            return new(0, 0, gridCells, null);

        if (image.Frames.Count == 1)
            return new(1, 0, gridCells, null);

        double totalMs = 0;
        for (var i = 0; i < image.Frames.Count; i++)
            totalMs += GetFrameDelayMs(image.Frames[i]);

        if (totalMs <= 0)
            return new(image.Frames.Count, 0, gridCells, null);

        var impliedFps = gridCells * 1000.0 / totalMs;
        return new(image.Frames.Count, totalMs, gridCells, impliedFps);
    }

    /// <summary>Reads grid dimensions embedded by <see cref="ExportAnimatedImageToSpriteSheetPngAsync" /> (PNG tEXt chunk).</summary>
    public static bool TryReadSpriteSheetGridFromImageBytes(byte[] imageBytes, out int rows, out int columns)
    {
        rows = 1;
        columns = 1;
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            return false;

        try {
            using var image = Image.Load<Rgba32>(new MemoryStream(imageBytes, false));
            var png = image.Metadata.GetPngMetadata();
            foreach (var td in png.TextData) {
                if (td.Keyword != PngGridTextKeyword || string.IsNullOrWhiteSpace(td.Value))
                    continue;

                using var doc = JsonDocument.Parse(td.Value);
                var root = doc.RootElement;
                if (root.TryGetProperty("r", out var r) && root.TryGetProperty("c", out var c)) {
                    rows = Math.Clamp(r.GetInt32(), 1, 256);
                    columns = Math.Clamp(c.GetInt32(), 1, 4096);
                    return true;
                }
            }
        }
        catch {
            return false;
        }

        return false;
    }

    /// <summary>Expands <paramref name="sampled" /> to length <paramref name="targetCount" /> by duplicating entries when the grid has more cells than samples.</summary>
    public static IReadOnlyList<int> ExpandSampleIndicesToGrid(IReadOnlyList<int> sampled, int targetCount, SpriteGridPadMode mode)
    {
        ArgumentNullException.ThrowIfNull(sampled);
        var n = sampled.Count;
        if (n == 0)
            throw new ArgumentException("Sample list must not be empty.", nameof(sampled));

        targetCount = Math.Max(1, targetCount);
        if (targetCount < n)
            throw new ArgumentOutOfRangeException(nameof(targetCount), "Target count must be at least the number of samples.");

        if (targetCount == n)
            return sampled;

        return mode switch {
            SpriteGridPadMode.EndRepeatLast => ExpandEndRepeat(sampled, targetCount),
            SpriteGridPadMode.SymmetricBookends => ExpandSymmetric(sampled, targetCount),
            SpriteGridPadMode.StretchedUniform => ExpandStretchedUniform(sampled, targetCount),
            var _ => ExpandStretchedUniform(sampled, targetCount)
        };
    }

    private static List<int> ExpandEndRepeat(IReadOnlyList<int> sampled, int m)
    {
        var n = sampled.Count;
        var list = new List<int>(m);
        list.AddRange(sampled);
        for (var i = 0; i < m - n; i++)
            list.Add(sampled[n - 1]);

        return list;
    }

    private static List<int> ExpandSymmetric(IReadOnlyList<int> sampled, int m)
    {
        var n = sampled.Count;
        var pad = m - n;
        var padFront = pad / 2;
        var padBack = pad - padFront;
        var list = new List<int>(m);
        for (var i = 0; i < padFront; i++)
            list.Add(sampled[0]);

        list.AddRange(sampled);
        for (var i = 0; i < padBack; i++)
            list.Add(sampled[n - 1]);

        return list;
    }

    private static List<int> ExpandStretchedUniform(IReadOnlyList<int> sampled, int m)
    {
        var n = sampled.Count;
        if (n == 1)
            return Enumerable.Repeat(sampled[0], m).ToList();

        var list = new List<int>(m);
        for (var k = 0; k < m; k++) {
            var listIdx = (int)Math.Round(k * (n - 1) / (double)(m - 1));
            listIdx = Math.Clamp(listIdx, 0, n - 1);
            list.Add(sampled[listIdx]);
        }

        return list;
    }

    /// <summary>
    /// Preview/export FPS from one animation loop: <c>frame_count / loop_duration</c>, clamped 1–60. For a single frame or missing timing, returns
    /// <see cref="DefaultSpriteSheetPlaybackFps" />.
    /// </summary>
    public static int ComputePlaybackFpsFromAnimatedStats(int sourceFrameCount, double loopDurationMs)
    {
        if (sourceFrameCount <= 1 || loopDurationMs <= 0)
            return DefaultSpriteSheetPlaybackFps;

        var fps = (int)Math.Round(sourceFrameCount * 1000.0 / loopDurationMs);
        return Math.Clamp(fps, 1, 60);
    }

    /// <summary>
    /// Target number of frames to place on the sheet for one loop: min(source frames, max(1, round(loop seconds × target fps))). Static or missing timing uses every source
    /// frame.
    /// </summary>
    public static int ComputeExtractSampleBudget(int sourceFrameCount, double loopDurationMs, int targetFps)
    {
        sourceFrameCount = Math.Max(1, sourceFrameCount);
        targetFps = Math.Clamp(targetFps, 1, 240);
        if (loopDurationMs <= 0 || sourceFrameCount == 1)
            return sourceFrameCount;

        var targetSamples = (int)Math.Round(loopDurationMs / 1000.0 * targetFps);
        targetSamples = Math.Max(1, targetSamples);
        return Math.Min(sourceFrameCount, targetSamples);
    }

    /// <summary>
    /// Picks row and column counts so rows × cols ≥ <paramref name="frameCount" />. Uneven totals leave extra cells at the end of the grid; export fills them using
    /// <see cref="SpriteGridPadMode" />. If <paramref name="desiredRowCount" /> is too small to fit the frames at max 4096 columns, row count is raised to the minimum needed.
    /// </summary>
    public static (int RowCount, int FramesPerRow) FitGridToRowCount(int frameCount, int desiredRowCount)
    {
        frameCount = Math.Max(1, frameCount);
        const int maxCols = 4096;
        const int maxRows = 256;
        var rows = Math.Clamp(desiredRowCount, 1, maxRows);
        var minRowsForWidth = (int)Math.Ceiling(frameCount / (double)maxCols);
        if (rows < minRowsForWidth)
            rows = Math.Clamp(minRowsForWidth, 1, maxRows);

        while (rows <= maxRows) {
            var cols = (int)Math.Ceiling(frameCount / (double)rows);
            if (cols > maxCols) {
                rows++;
                continue;
            }

            cols = Math.Clamp(cols, 1, maxCols);
            if (rows * cols >= frameCount)
                return (rows, cols);

            rows++;
        }

        var fallbackCols = Math.Clamp((int)Math.Ceiling(frameCount / (double)maxRows), 1, maxCols);
        return (maxRows, fallbackCols);
    }

    private static async Task<byte[]> ExportAnimatedImageToSpriteSheetPngCoreAsync(
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
        SpriteGridPadMode gridPadMode,
        CancellationToken ct)
    {
        rowCount = Math.Clamp(rowCount, 1, 256);
        framesPerRow = Math.Clamp(framesPerRow, 1, 4096);
        offsetX = Math.Clamp(offsetX, 0, 8192);
        offsetY = Math.Clamp(offsetY, 0, 8192);
        padLeft = Math.Clamp(padLeft, 0, 4096);
        padRight = Math.Clamp(padRight, 0, 4096);
        padTop = Math.Clamp(padTop, 0, 4096);
        padBottom = Math.Clamp(padBottom, 0, 4096);
        var maxCells = rowCount * framesPerRow;
        if (maxCells == 0)
            throw new InvalidOperationException("Grid must have at least one cell.");

        sampleBudget = Math.Clamp(sampleBudget, 1, maxCells);
        await using var sourceStream = new MemoryStream(imageBytes, false);
        using var image = await Image.LoadAsync<Rgba32>(sourceStream, ct);
        if (image.Frames.Count == 0)
            throw new InvalidOperationException("Image has no frames.");

        var sampledIndices = SampleFrameIndices(image, sampleBudget);
        if (sampledIndices.Count == 0)
            throw new InvalidOperationException("No frames were sampled from the animation.");

        var expandedIndices = ExpandSampleIndicesToGrid(sampledIndices, maxCells, gridPadMode);
        var frameImages = new List<Image<Rgba32>>(expandedIndices.Count);
        try {
            foreach (var index in expandedIndices) {
                ct.ThrowIfCancellationRequested();
                var safeIndex = Math.Clamp(index, 0, image.Frames.Count - 1);
                frameImages.Add(image.Frames.CloneFrame(safeIndex));
            }

            var maxW = frameImages.Max(f => f.Width);
            var maxH = frameImages.Max(f => f.Height);
            var cellW = maxW + padLeft + padRight;
            var cellH = maxH + padTop + padBottom;
            if (cellW <= 0 || cellH <= 0)
                throw new InvalidOperationException("Computed cell size is invalid.");

            var sheetW = offsetX + framesPerRow * cellW;
            var sheetH = offsetY + rowCount * cellH;
            if (sheetW <= 0 || sheetH <= 0 || sheetW > 16384 || sheetH > 16384)
                throw new InvalidOperationException("Spritesheet dimensions are out of range.");

            using var sheet = new Image<Rgba32>(sheetW, sheetH);
            sheet.Mutate(c => c.BackgroundColor(Color.Transparent));
            var gridJson = JsonSerializer.Serialize(new { r = rowCount, c = framesPerRow });
            sheet.Metadata.GetPngMetadata().TextData.Add(new(PngGridTextKeyword, gridJson, string.Empty, string.Empty));
            for (var i = 0; i < maxCells; i++) {
                ct.ThrowIfCancellationRequested();
                var col = i % framesPerRow;
                var row = i / framesPerRow;
                if (row >= rowCount)
                    break;

                var cell = frameImages[i];
                var destX = offsetX + col * cellW + padLeft + (maxW - cell.Width) / 2;
                var destY = offsetY + row * cellH + padTop + (maxH - cell.Height) / 2;
                sheet.Mutate(ctx => ctx.DrawImage(cell, new Point(destX, destY), 1f));
            }

            await using var outputStream = new MemoryStream();
            await sheet.SaveAsPngAsync(outputStream, new(), ct);
            return outputStream.ToArray();
        }
        finally {
            foreach (var fi in frameImages)
                fi.Dispose();
        }
    }

    private static List<int> SampleFrameIndices(Image<Rgba32> image, int sampleCount)
    {
        if (sampleCount <= 0)
            return [];

        var frameCount = image.Frames.Count;
        if (frameCount == 1)
            return Enumerable.Repeat(0, sampleCount).ToList();

        if (sampleCount < frameCount) {
            var delaysMs = new List<double>(frameCount);
            for (var i = 0; i < frameCount; i++)
                delaysMs.Add(GetFrameDelayMs(image.Frames[i]));

            var totalMs = delaysMs.Sum();
            if (totalMs <= 0)
                return Enumerable.Repeat(0, sampleCount).ToList();

            var indices = new List<int>(sampleCount);
            for (var i = 0; i < sampleCount; i++) {
                var t = (i + 0.5) * totalMs / sampleCount;
                indices.Add(GetFrameIndexAtTime(t, delaysMs));
            }

            return indices;
        }

        var ordered = new List<int>(sampleCount);
        for (var i = 0; i < sampleCount; i++)
            ordered.Add(Math.Min(i, frameCount - 1));

        return ordered;
    }

    private static int GetFrameIndexAtTime(double timeMs, IReadOnlyList<double> delaysMs)
    {
        var acc = 0d;
        for (var i = 0; i < delaysMs.Count; i++) {
            var next = acc + delaysMs[i];
            if (timeMs < next || i == delaysMs.Count - 1)
                return i;

            acc = next;
        }

        return delaysMs.Count - 1;
    }

    private static double GetFrameDelayMs(ImageFrame frame)
    {
        var gifMeta = frame.Metadata.GetGifMetadata();
        if (gifMeta != null)
            return gifMeta.FrameDelay <= 0 ? 100 : gifMeta.FrameDelay * 10d;

        if (frame.Metadata.TryGetWebpFrameMetadata(out var webpMeta))
            return webpMeta.FrameDelay <= 0 ? 100 : webpMeta.FrameDelay;

        return 100;
    }

    private static string FormatErrors(IEnumerable<Error>? errors)
        => errors == null || !errors.Any()
            ? "Unknown error."
            : string.Join(Environment.NewLine, errors.Where(error => !string.IsNullOrWhiteSpace(error.Message)).Select(error => $"{error.Code}: {error.Message}"));
}